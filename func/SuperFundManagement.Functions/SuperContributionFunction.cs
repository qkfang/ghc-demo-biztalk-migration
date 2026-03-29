using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SuperFundManagement.Functions.Models;
using SuperFundManagement.Functions.Services;

namespace SuperFundManagement.Functions;

/// <summary>
/// Azure Function replacement for the BizTalk SuperContributionOrchestration.
/// Receives an HTTP POST with a SuperContributionRequest XML body,
/// transforms it using ContributionTransformService, and returns the
/// FundAllocationInstruction XML — mirroring the BizTalk receive/send pipeline pair.
/// </summary>
public class SuperContributionFunction
{
    private readonly ContributionTransformService _transformService;
    private readonly ILogger<SuperContributionFunction> _logger;

    public SuperContributionFunction(
        ContributionTransformService transformService,
        ILogger<SuperContributionFunction> logger)
    {
        _transformService = transformService;
        _logger = logger;
    }

    [Function("ProcessContribution")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "contributions")] HttpRequestData req)
    {
        _logger.LogInformation("Processing super contribution request.");

        // ── Parse incoming XML (BizTalk receive pipeline equivalent) ─────────
        SuperContributionRequest request;
        try
        {
            var body = await req.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Request body is empty.");
                return badRequest;
            }

            var serializer = new XmlSerializer(typeof(SuperContributionRequest));
            using var reader = new StringReader(body);
            request = (SuperContributionRequest?)serializer.Deserialize(reader)
                      ?? throw new InvalidOperationException("Failed to deserialize request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize SuperContributionRequest.");
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync($"Invalid XML: {ex.Message}");
            return badRequest;
        }

        _logger.LogInformation("Processing contribution {ContributionId} for employer {EmployerId}.",
            request.ContributionId, request.EmployerId);

        // ── Transform (BizTalk map + orchestration equivalent) ───────────────
        var allocation = _transformService.Transform(request);

        // ── Serialize output XML (BizTalk send pipeline equivalent) ─────────
        var outputSerializer = new XmlSerializer(typeof(FundAllocationInstruction));
        var xmlSettings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true,
            OmitXmlDeclaration = false
        };

        using var memStream = new MemoryStream();
        using (var xmlWriter = XmlWriter.Create(memStream, xmlSettings))
        {
            outputSerializer.Serialize(xmlWriter, allocation);
        }

        var xmlOutput = Encoding.UTF8.GetString(memStream.ToArray());

        _logger.LogInformation("Allocation {AllocationId} created successfully.", allocation.AllocationId);

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/xml; charset=utf-8");
        await response.WriteStringAsync(xmlOutput);
        return response;
    }
}
