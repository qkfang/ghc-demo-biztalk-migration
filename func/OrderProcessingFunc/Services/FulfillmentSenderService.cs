using System.Text;
using System.Xml.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrderProcessingFunc.Models;

namespace OrderProcessingFunc.Services;

/// <summary>
/// Sends a fulfillment order to the downstream HTTP service as XML.
/// Replaces the BizTalk FulfillmentHttpSend send port and HttpSendPipeline.
/// </summary>
public class FulfillmentSenderService : IFulfillmentSenderService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FulfillmentSenderService> _logger;
    private readonly string _fulfillmentServiceUrl;
    private static readonly XmlSerializer _serializer = new(typeof(FulfillmentOrder));

    public FulfillmentSenderService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<FulfillmentSenderService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _fulfillmentServiceUrl = configuration["FulfillmentServiceUrl"]
            ?? throw new InvalidOperationException("FulfillmentServiceUrl configuration is required.");
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> SendAsync(FulfillmentOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);

        _logger.LogInformation(
            "Sending fulfillment order {FulfillmentId} to {Url}",
            order.FulfillmentId,
            _fulfillmentServiceUrl);

        var xmlBody = SerializeToXml(order);

        var client = _httpClientFactory.CreateClient(nameof(FulfillmentSenderService));
        using var content = new StringContent(xmlBody, Encoding.UTF8, "application/xml");

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(_fulfillmentServiceUrl, content);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "HTTP request to fulfillment service failed for order {FulfillmentId}",
                order.FulfillmentId);
            throw;
        }

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "Fulfillment service returned {StatusCode} for order {FulfillmentId}. Body: {Body}",
                (int)response.StatusCode,
                order.FulfillmentId,
                responseBody);
        }
        else
        {
            _logger.LogInformation(
                "Fulfillment order {FulfillmentId} accepted by downstream service with status {StatusCode}",
                order.FulfillmentId,
                (int)response.StatusCode);
        }

        return response;
    }

    private static string SerializeToXml(FulfillmentOrder order)
    {
        var sb = new StringBuilder();
        using var writer = new System.IO.StringWriter(sb);
        _serializer.Serialize(writer, order);
        return sb.ToString();
    }
}
