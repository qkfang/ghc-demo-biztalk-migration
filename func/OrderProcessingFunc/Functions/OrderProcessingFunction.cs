using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OrderProcessingFunc.Models;
using OrderProcessingFunc.Services;

namespace OrderProcessingFunc.Functions;

/// <summary>
/// Azure Function entry point for order processing.
/// Replaces the BizTalk OrderProcessingOrchestration — receiving HTTP POST,
/// transforming via OrderToFulfillmentMap, and forwarding to downstream service.
/// </summary>
public class OrderProcessingFunction
{
    private readonly IOrderTransformService _transformService;
    private readonly IFulfillmentSenderService _senderService;
    private readonly ILogger<OrderProcessingFunction> _logger;
    private static readonly XmlSerializer _orderSerializer = new(typeof(SourceOrder));

    public OrderProcessingFunction(
        IOrderTransformService transformService,
        IFulfillmentSenderService senderService,
        ILogger<OrderProcessingFunction> logger)
    {
        _transformService = transformService;
        _senderService = senderService;
        _logger = logger;
    }

    /// <summary>
    /// Processes an incoming XML order request and forwards it as a fulfillment order.
    /// POST /api/orders
    /// </summary>
    [Function(nameof(ProcessOrder))]
    public async Task<HttpResponseData> ProcessOrder(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] HttpRequestData req)
    {
        _logger.LogInformation("ProcessOrder function triggered. ContentType: {ContentType}",
            req.Headers.TryGetValues("Content-Type", out var ct) ? string.Join(",", ct) : "unknown");

        // 1. Read and deserialize XML body
        SourceOrder order;
        try
        {
            var body = await new StreamReader(req.Body, Encoding.UTF8).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return await BadRequest(req, "Request body is empty.");
            }

            using var reader = XmlReader.Create(new StringReader(body));
            order = (SourceOrder)_orderSerializer.Deserialize(reader)!;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize XML order payload.");
            return await BadRequest(req, $"Invalid XML payload: {ex.Message}");
        }

        // 2. Validate required fields
        var validationError = ValidateOrder(order);
        if (validationError is not null)
        {
            _logger.LogWarning("Order validation failed: {Error}", validationError);
            return await BadRequest(req, validationError);
        }

        _logger.LogInformation("Processing order {OrderId} for customer {CustomerId}",
            order.OrderId, order.CustomerId);

        // 3. Transform SourceOrder → FulfillmentOrder
        FulfillmentOrder fulfillmentOrder;
        try
        {
            fulfillmentOrder = _transformService.Transform(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transform order {OrderId}", order.OrderId);
            return await InternalError(req, "Order transformation failed.");
        }

        // 4. Send to downstream fulfillment service
        HttpResponseMessage downstreamResponse;
        try
        {
            downstreamResponse = await _senderService.SendAsync(fulfillmentOrder);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to reach fulfillment service for order {OrderId}", order.OrderId);
            return await BadGateway(req, "Downstream fulfillment service is unavailable.");
        }

        if (!downstreamResponse.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Fulfillment service rejected order {OrderId} with status {StatusCode}",
                order.OrderId, (int)downstreamResponse.StatusCode);
            return await BadGateway(req,
                $"Downstream fulfillment service returned {(int)downstreamResponse.StatusCode}.");
        }

        // 5. Return 202 Accepted with FulfillmentId
        _logger.LogInformation("Order {OrderId} fulfilled as {FulfillmentId}",
            order.OrderId, fulfillmentOrder.FulfillmentId);

        var accepted = req.CreateResponse(HttpStatusCode.Accepted);
        accepted.Headers.Add("Content-Type", "application/json");
        await accepted.WriteStringAsync(JsonSerializer.Serialize(new
        {
            fulfillmentId = fulfillmentOrder.FulfillmentId,
            sourceOrderRef = fulfillmentOrder.SourceOrderRef,
            status = fulfillmentOrder.Status
        }));
        return accepted;
    }

    private static string? ValidateOrder(SourceOrder order)
    {
        if (string.IsNullOrWhiteSpace(order.OrderId))
            return "OrderId is required.";
        if (string.IsNullOrWhiteSpace(order.CustomerId))
            return "CustomerId is required.";
        if (order.Items?.Item is null || order.Items.Item.Count == 0)
            return "Order must contain at least one item.";
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
