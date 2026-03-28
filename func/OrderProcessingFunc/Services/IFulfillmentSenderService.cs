using OrderProcessingFunc.Models;

namespace OrderProcessingFunc.Services;

/// <summary>
/// Defines the contract for sending a fulfillment order to the downstream HTTP service.
/// Replaces the BizTalk FulfillmentHttpSend send port.
/// </summary>
public interface IFulfillmentSenderService
{
    /// <summary>
    /// Serializes the fulfillment order as XML and POSTs it to the configured downstream service URL.
    /// </summary>
    /// <param name="order">The fulfillment order to send.</param>
    /// <returns>The HTTP response from the downstream service.</returns>
    Task<HttpResponseMessage> SendAsync(FulfillmentOrder order);
}
