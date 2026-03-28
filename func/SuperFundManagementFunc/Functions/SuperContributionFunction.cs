using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SuperFundManagementFunc.Models;
using SuperFundManagementFunc.Services;

namespace SuperFundManagementFunc.Functions;

/// <summary>
/// Azure Function entry point for superannuation contribution processing.
/// Replaces the BizTalk SuperContributionOrchestration — receiving an HTTP POST with
/// an employer's contribution XML, transforming it via ContributionToAllocationMap,
/// and forwarding it to the fund administration platform.
/// </summary>
public class SuperContributionFunction
{
    private readonly IContributionTransformService _transformService;
    private readonly IFundAllocationSenderService _senderService;
    private readonly ILogger<SuperContributionFunction> _logger;
    private static readonly XmlSerializer _contributionSerializer = new(typeof(SuperContribution));

    public SuperContributionFunction(
        IContributionTransformService transformService,
        IFundAllocationSenderService senderService,
        ILogger<SuperContributionFunction> logger)
    {
        _transformService = transformService;
        _senderService = senderService;
        _logger = logger;
    }

    /// <summary>
    /// Processes an incoming XML superannuation contribution request and forwards it as a fund allocation instruction.
    /// POST /api/contributions
    /// </summary>
    [Function(nameof(ProcessContribution))]
    public async Task<HttpResponseData> ProcessContribution(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "contributions")] HttpRequestData req)
    {
        _logger.LogInformation("ProcessContribution function triggered. ContentType: {ContentType}",
            req.Headers.TryGetValues("Content-Type", out var ct) ? string.Join(",", ct) : "unknown");

        // 1. Read and deserialize XML body
        SuperContribution contribution;
        try
        {
            var body = await new StreamReader(req.Body, Encoding.UTF8).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return await BadRequest(req, "Request body is empty.");
            }

            using var reader = XmlReader.Create(new StringReader(body));
            contribution = (SuperContribution)_contributionSerializer.Deserialize(reader)!;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize XML contribution payload.");
            return await BadRequest(req, $"Invalid XML payload: {ex.Message}");
        }

        // 2. Validate required fields
        var validationError = ValidateContribution(contribution);
        if (validationError is not null)
        {
            _logger.LogWarning("Contribution validation failed: {Error}", validationError);
            return await BadRequest(req, validationError);
        }

        _logger.LogInformation("Processing contribution {ContributionId} for employer {EmployerId}",
            contribution.ContributionId, contribution.EmployerId);

        // 3. Transform SuperContribution → FundAllocation
        FundAllocation allocation;
        try
        {
            allocation = _transformService.Transform(contribution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transform contribution {ContributionId}", contribution.ContributionId);
            return await InternalError(req, "Contribution transformation failed.");
        }

        // 4. Send to fund administration platform
        HttpResponseMessage platformResponse;
        try
        {
            platformResponse = await _senderService.SendAsync(allocation);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to reach fund administration platform for contribution {ContributionId}", contribution.ContributionId);
            return await BadGateway(req, "Fund administration platform is unavailable.");
        }

        if (!platformResponse.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Fund administration platform rejected contribution {ContributionId} with status {StatusCode}",
                contribution.ContributionId, (int)platformResponse.StatusCode);
            return await BadGateway(req,
                $"Fund administration platform returned {(int)platformResponse.StatusCode}.");
        }

        // 5. Return 202 Accepted with AllocationId
        _logger.LogInformation("Contribution {ContributionId} allocated as {AllocationId}",
            contribution.ContributionId, allocation.AllocationId);

        var accepted = req.CreateResponse(HttpStatusCode.Accepted);
        accepted.Headers.Add("Content-Type", "application/json");
        await accepted.WriteStringAsync(JsonSerializer.Serialize(new
        {
            allocationId = allocation.AllocationId,
            sourceContributionRef = allocation.SourceContributionRef,
            status = allocation.Status
        }));
        return accepted;
    }

    private static string? ValidateContribution(SuperContribution contribution)
    {
        if (string.IsNullOrWhiteSpace(contribution.ContributionId))
            return "ContributionId is required.";
        if (string.IsNullOrWhiteSpace(contribution.EmployerId))
            return "EmployerId is required.";
        if (contribution.Members?.Member is null || contribution.Members.Member.Count == 0)
            return "Contribution must contain at least one member.";
        return null;
    }

    private static async Task<HttpResponseData> BadRequest(HttpRequestData req, string message)
    {
        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(new { error = message }));
        return response;
    }

    private static async Task<HttpResponseData> BadGateway(HttpRequestData req, string message)
    {
        var response = req.CreateResponse(HttpStatusCode.BadGateway);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(new { error = message }));
        return response;
    }

    private static async Task<HttpResponseData> InternalError(HttpRequestData req, string message)
    {
        var response = req.CreateResponse(HttpStatusCode.InternalServerError);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(new { error = message }));
        return response;
    }
}
