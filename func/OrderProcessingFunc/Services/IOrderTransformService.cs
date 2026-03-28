using OrderProcessingFunc.Models;

namespace OrderProcessingFunc.Services;

/// <summary>
/// Defines the contract for transforming a source order into a fulfillment order.
/// Replaces the BizTalk OrderToFulfillmentMap.btm transformation.
/// </summary>
public interface IOrderTransformService
{
    /// <summary>
    /// Transforms a <see cref="SourceOrder"/> into a <see cref="FulfillmentOrder"/>,
    /// applying all business rules equivalent to the BizTalk map functoids.
    /// </summary>
    /// <param name="order">The incoming source order to transform.</param>
    /// <returns>A fully populated <see cref="FulfillmentOrder"/> ready to dispatch.</returns>
    FulfillmentOrder Transform(SourceOrder order);
}
