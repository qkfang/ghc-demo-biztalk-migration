using System.Text;
using System.Xml.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SuperFundManagementFunc.Models;

namespace SuperFundManagementFunc.Services;

/// <summary>
/// Sends a fund allocation instruction to the downstream fund administration platform as XML.
/// Replaces the BizTalk AllocationHttpSend send port and HttpSendPipeline.
/// </summary>
public class FundAllocationSenderService : IFundAllocationSenderService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FundAllocationSenderService> _logger;
    private readonly string _fundAdminPlatformUrl;
    private static readonly XmlSerializer _serializer = new(typeof(FundAllocation));

    public FundAllocationSenderService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<FundAllocationSenderService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _fundAdminPlatformUrl = configuration["FundAdminPlatformUrl"]
            ?? throw new InvalidOperationException("FundAdminPlatformUrl configuration is required.");
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> SendAsync(FundAllocation allocation)
    {
        ArgumentNullException.ThrowIfNull(allocation);

        _logger.LogInformation(
            "Sending fund allocation instruction {AllocationId} to {Url}",
            allocation.AllocationId,
            _fundAdminPlatformUrl);

        var xmlBody = SerializeToXml(allocation);

        var client = _httpClientFactory.CreateClient(nameof(FundAllocationSenderService));
        using var content = new StringContent(xmlBody, Encoding.UTF8, "application/xml");

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(_fundAdminPlatformUrl, content);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "HTTP request to fund administration platform failed for allocation {AllocationId}",
                allocation.AllocationId);
            throw;
        }

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "Fund administration platform returned {StatusCode} for allocation {AllocationId}. Body: {Body}",
                (int)response.StatusCode,
                allocation.AllocationId,
                responseBody);
        }
        else
        {
            _logger.LogInformation(
                "Fund allocation instruction {AllocationId} accepted by fund administration platform with status {StatusCode}",
                allocation.AllocationId,
                (int)response.StatusCode);
        }

        return response;
    }

    private static string SerializeToXml(FundAllocation allocation)
    {
        var sb = new StringBuilder();
        using var writer = new System.IO.StringWriter(sb);
        _serializer.Serialize(writer, allocation);
        return sb.ToString();
    }
}
