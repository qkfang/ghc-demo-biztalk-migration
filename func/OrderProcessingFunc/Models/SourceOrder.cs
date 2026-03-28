using System.Xml.Serialization;

namespace OrderProcessingFunc.Models;

/// <summary>
/// Represents an incoming order in the legacy OrderRequest XML format.
/// Mirrors the SourceOrderSchema.xsd BizTalk schema.
/// </summary>
[XmlRoot("OrderRequest", Namespace = "http://OrderProcessing.Schemas.SourceOrder")]
public class SourceOrder
{
    [XmlElement("OrderId")]
    public string OrderId { get; set; } = string.Empty;

    [XmlElement("CustomerId")]
    public string CustomerId { get; set; } = string.Empty;

    [XmlElement("CustomerName")]
    public string CustomerName { get; set; } = string.Empty;

    [XmlElement("CustomerEmail")]
    public string CustomerEmail { get; set; } = string.Empty;

    [XmlElement("OrderDate")]
    public DateTime OrderDate { get; set; }

    [XmlElement("Items")]
    public OrderItems Items { get; set; } = new();

    [XmlElement("TotalAmount")]
    public decimal TotalAmount { get; set; }

    [XmlElement("Currency")]
    public string Currency { get; set; } = "USD";

    [XmlElement("ShippingAddress")]
    public ShippingAddress ShippingAddress { get; set; } = new();
}

/// <summary>
/// Container for line items in the source order.
/// </summary>
public class OrderItems
{
    [XmlElement("Item")]
    public List<OrderItem> Item { get; set; } = new();
}

/// <summary>
/// A single line item in the source order.
/// </summary>
public class OrderItem
{
    [XmlElement("ProductCode")]
    public string ProductCode { get; set; } = string.Empty;

    [XmlElement("ProductName")]
    public string ProductName { get; set; } = string.Empty;

    [XmlElement("Quantity")]
    public int Quantity { get; set; }

    [XmlElement("UnitPrice")]
    public decimal UnitPrice { get; set; }
}

/// <summary>
/// Shipping address from the source order.
/// </summary>
public class ShippingAddress
{
    [XmlElement("Street")]
    public string Street { get; set; } = string.Empty;

    [XmlElement("City")]
    public string City { get; set; } = string.Empty;

    [XmlElement("State")]
    public string State { get; set; } = string.Empty;

    [XmlElement("ZipCode")]
    public string ZipCode { get; set; } = string.Empty;

    [XmlElement("Country")]
    public string Country { get; set; } = string.Empty;
}
