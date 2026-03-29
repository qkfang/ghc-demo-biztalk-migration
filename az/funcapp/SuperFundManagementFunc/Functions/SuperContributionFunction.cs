using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using SuperFundManagement.Functions.Models;
using SuperFundManagement.Functions.Services;

namespace SuperFundManagement.Functions.Functions;

public class SuperContributionFunction
{
    private readonly IContributionTransformService _transformService;
    private readonly IFundAllocationSenderService  _senderService;
    private readonly ILogger<SuperContributionFunction> _logger;

    public SuperContributionFunction(
        IContributionTransformService transformService,
        IFundAllocationSenderService  senderService,
        ILogger<SuperContributionFunction> logger)
    {
        _transformService = transformService;
        _senderService    = senderService;
        _logger           = logger;
    }

    [Function("SuperContribution")]
    [OpenApiOperation(operationId: "SubmitSuperContribution", tags: ["SuperFundManagement"],
        Summary = "Submit a super contribution request",
        Description = "Receives an XML SuperContributionRequest, transforms it to a FundAllocationInstruction, and forwards it to the fund administration API.",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiRequestBody(contentType: "application/xml", bodyType: typeof(SuperContributionRequest),
        Required = true,
        Description = "XML payload conforming to the SuperContributionRequest schema")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Accepted, contentType: "application/json", bodyType: typeof(object),
        Summary = "Accepted",
        Description = "Contribution accepted and allocation dispatched. Returns allocation summary.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string),
        Summary = "Bad Request",
        Description = "Invalid XML or transformation error.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadGateway, contentType: "text/plain", bodyType: typeof(string),
        Summary = "Bad Gateway",
        Description = "Downstream fund admin API returned an error.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "SuperFundManagement/Receive")]
        HttpRequestData req)
    {
        _logger.LogInformation("Received SuperContribution request");

        // 1. Deserialize XML body
        SuperContributionRequest contribution;
        try
        {
            var serializer = new XmlSerializer(typeof(SuperContributionRequest));
            using var reader = XmlReader.Create(req.Body);
            contribution = (SuperContributionRequest)serializer.Deserialize(reader)!;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize request body");
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync($"Invalid XML: {ex.Message}");
            return badRequest;
        }

        // 2. Transform
        FundAllocationInstruction instruction;
        try
        {
            instruction = _transformService.Transform(contribution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transform failed");
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync($"Transform error: {ex.Message}");
            return badRequest;
        }

        // 3. Send to fund admin
        HttpResponseMessage downstreamResponse;
        try
        {
            downstreamResponse = await _senderService.SendAsync(instruction);
            downstreamResponse.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send allocation to fund admin API");
            var badGateway = req.CreateResponse(HttpStatusCode.BadGateway);
            await badGateway.WriteStringAsync($"Downstream error: {ex.Message}");
            return badGateway;
        }

        // 4. Return 202 Accepted
        var response = req.CreateResponse(HttpStatusCode.Accepted);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");

        var summary = new
        {
            AllocationId   = instruction.AllocationId,
            Status         = instruction.Status,
            MemberCount    = instruction.MemberAllocations.Allocation.Count,
            TotalAllocated = instruction.TotalAllocated
        };
        await response.WriteStringAsync(JsonSerializer.Serialize(summary));
        return response;
    }
}
