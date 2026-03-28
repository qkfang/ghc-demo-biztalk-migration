using OrderProcessingFunc.Models;
using OrderProcessingFunc.Services;
using Xunit;

namespace OrderProcessingFunc.Tests.Services;

public class OrderTransformServiceTests
{
    private readonly OrderTransformService _sut = new();

    private static SourceOrder BuildSampleOrder() => new()
    {
        OrderId = "ORD-001",
        CustomerId = "CUST-42",
        CustomerName = "Jane Smith",
        CustomerEmail = "jane.smith@example.com",
        OrderDate = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc),
        TotalAmount = 149.97m,
        Currency = "USD",
        Items = new OrderItems
        {
            Item = new List<OrderItem>
            {
                new() { ProductCode = "SKU-A", ProductName = "Widget Pro", Quantity = 3, UnitPrice = 29.99m },
                new() { ProductCode = "SKU-B", ProductName = "Gadget X",   Quantity = 2, UnitPrice = 35.00m }
            }
        },
        ShippingAddress = new ShippingAddress
        {
            Street = "123 Main St",
            City = "Springfield",
            State = "IL",
            ZipCode = "62701",
            Country = "US"
        }
    };

    [Fact]
    public void Transform_SetsCorrectFulfillmentIdPrefix()
    {
        var order = BuildSampleOrder();

        var result = _sut.Transform(order);

        Assert.StartsWith("FF-", result.FulfillmentId);
        Assert.Equal("FF-ORD-001", result.FulfillmentId);
    }

    [Fact]
    public void Transform_MapsCustomerDetailsCorrectly()
    {
        var order = BuildSampleOrder();

        var result = _sut.Transform(order);

        Assert.Equal(order.CustomerId, result.CustomerDetails.Id);
        Assert.Equal(order.CustomerName, result.CustomerDetails.Name);
        Assert.Equal(order.CustomerEmail, result.CustomerDetails.Email);
    }

    [Fact]
    public void Transform_MapsLineItemsCorrectly()
    {
        var order = BuildSampleOrder();

        var result = _sut.Transform(order);

        Assert.Equal(2, result.LineItems.LineItem.Count);

        var first = result.LineItems.LineItem[0];
        Assert.Equal("SKU-A", first.SKU);
        Assert.Equal("Widget Pro", first.Description);
        Assert.Equal(3, first.Qty);
        Assert.Equal(29.99m, first.Price);
        Assert.Equal(89.97m, first.LineTotal); // 3 * 29.99

        var second = result.LineItems.LineItem[1];
        Assert.Equal("SKU-B", second.SKU);
        Assert.Equal(70.00m, second.LineTotal); // 2 * 35.00
    }

    [Fact]
    public void Transform_SetsStatusToPending()
    {
        var order = BuildSampleOrder();

        var result = _sut.Transform(order);

        Assert.Equal("PENDING", result.Status);
    }

    [Fact]
    public void Transform_MapsShippingAddressCorrectly()
    {
        var order = BuildSampleOrder();

        var result = _sut.Transform(order);

        Assert.Equal("123 Main St", result.ShipTo.AddressLine1);
        Assert.Equal("Springfield", result.ShipTo.City);
        Assert.Equal("IL", result.ShipTo.StateProvince);
        Assert.Equal("62701", result.ShipTo.PostalCode);
        Assert.Equal("US", result.ShipTo.CountryCode);
    }

    [Fact]
    public void Transform_PreservesOrderTotal()
    {
        var order = BuildSampleOrder();

        var result = _sut.Transform(order);

        Assert.Equal(149.97m, result.OrderTotal);
        Assert.Equal("USD", result.CurrencyCode);
    }

    [Fact]
    public void Transform_SetsSourceOrderRef()
    {
        var order = BuildSampleOrder();

        var result = _sut.Transform(order);

        Assert.Equal(order.OrderId, result.SourceOrderRef);
    }

    [Fact]
    public void Transform_PreservesRequestedDate()
    {
        var order = BuildSampleOrder();

        var result = _sut.Transform(order);

        Assert.Equal(order.OrderDate, result.RequestedDate);
    }

    [Fact]
    public void Transform_ThrowsArgumentNullException_WhenOrderIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.Transform(null!));
    }
}
