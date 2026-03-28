using OrderProcessingFunc.Models;

namespace OrderProcessingFunc.Services;

/// <summary>
/// Implements the order-to-fulfillment transformation logic.
/// This is the Azure Functions equivalent of the BizTalk OrderToFulfillmentMap.btm,
/// including the String Concatenate functoid (FF- prefix) and Multiplication functoid (LineTotal).
/// </summary>
public class OrderTransformService : IOrderTransformService
{
    private const string FulfillmentIdPrefix = "FF-";
    private const string DefaultStatus = "PENDING";

    /// <inheritdoc />
    public FulfillmentOrder Transform(SourceOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);

        return new FulfillmentOrder
        {
            // BizTalk equivalent: String Concatenate functoid("FF-", OrderId)
            FulfillmentId = FulfillmentIdPrefix + order.OrderId,

            // Direct field mappings
            SourceOrderRef = order.OrderId,
            RequestedDate = order.OrderDate,
            OrderTotal = order.TotalAmount,
            CurrencyCode = order.Currency,
            Status = DefaultStatus,

            CustomerDetails = new CustomerDetails
            {
                Id = order.CustomerId,
                Name = order.CustomerName,
                Email = order.CustomerEmail
            },

            // BizTalk equivalent: Looping functoid over Items/Item → LineItems/LineItem
            LineItems = new FulfillmentLineItems
            {
                LineItem = order.Items.Item.Select(item => new LineItem
                {
                    SKU = item.ProductCode,
                    Description = item.ProductName,
                    Qty = item.Quantity,
                    Price = item.UnitPrice,
                    // BizTalk equivalent: Multiplication functoid(Quantity, UnitPrice)
                    LineTotal = item.Quantity * item.UnitPrice
                }).ToList()
            },

            ShipTo = new ShipTo
            {
                AddressLine1 = order.ShippingAddress.Street,
                City = order.ShippingAddress.City,
                StateProvince = order.ShippingAddress.State,
                PostalCode = order.ShippingAddress.ZipCode,
                CountryCode = order.ShippingAddress.Country
            }
        };
    }
}
