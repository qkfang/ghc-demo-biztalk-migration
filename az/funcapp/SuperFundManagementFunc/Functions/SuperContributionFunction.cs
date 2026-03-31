using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace SuperFundManagement.Functions;

public class SuperContributionFunction
{
    private readonly IContributionTransformService _transformService;
    private readonly IFundAllocationSenderService _senderService;
    private readonly ILogger<SuperContributionFunction> _logger;

    public SuperContributionFunction(
        IContributionTransformService transformService,
        IFundAllocationSenderService senderService,
        ILogger<SuperContributionFunction> logger)
    {
        _transformService = transformService;
        _senderService = senderService;
        _logger = logger;
    }

    [Function("SuperContribution")]
    [OpenApiOperation(operationId: "SuperContribution", tags: new[] { "SuperContribution" }, Summary = "Process a super contribution and send allocation to fund admin")]
    [OpenApiRequestBody("application/xml", typeof(string), Description = "SuperContributionRequest XML payload")]
    [OpenApiResponseWithBody(HttpStatusCode.Accepted, "application/json", typeof(object), Description = "Allocation accepted")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "supercontribution")] HttpRequestData req)
    {
        _logger.LogInformation("SuperContribution function received a request.");

        SuperContributionRequest? request;
        try
        {
            var serializer = new XmlSerializer(typeof(SuperContributionRequest));
            using var reader = new StreamReader(req.Body);
            var xml = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(xml))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Request body is empty.");
                return badRequest;
            }
            using var xmlReader = XmlReader.Create(new StringReader(xml));
            request = (SuperContributionRequest?)serializer.Deserialize(xmlReader);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid XML in request body.");
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync($"Invalid XML: {ex.Message}");
            return badRequest;
        }
        catch (XmlException ex)
        {
            _logger.LogWarning(ex, "Malformed XML in request body.");
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync($"Malformed XML: {ex.Message}");
            return badRequest;
        }

        if (request == null)
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("Could not deserialize request body.");
            return badRequest;
        }

        _logger.LogInformation("Deserialized contribution {ContributionId}", request.ContributionId);

        var instruction = _transformService.Transform(request);
        _logger.LogInformation("Transformed to allocation {AllocationId}", instruction.AllocationId);

        try
        {
            await _senderService.SendAsync(instruction);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to send allocation to fund admin.");
            var badGateway = req.CreateResponse(HttpStatusCode.BadGateway);
            await badGateway.WriteStringAsync($"Failed to forward allocation: {ex.Message}");
            return badGateway;
        }

        _logger.LogInformation("Allocation {AllocationId} sent successfully.", instruction.AllocationId);

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        response.Headers.Add("Content-Type", "application/json");
        var responseBody = JsonSerializer.Serialize(new
        {
            allocationId = instruction.AllocationId,
            status = instruction.Status
        });
        await response.WriteStringAsync(responseBody);
        return response;
    }
}
