using System.Xml.Serialization;

namespace OrderProcessingFunc.Models;

/// <summary>
/// Represents the outgoing fulfillment order in the modern FulfillmentOrder XML format.
/// Mirrors the TargetFulfillmentSchema.xsd BizTalk schema.
/// </summary>
[XmlRoot("FulfillmentOrder", Namespace = "http://OrderProcessing.Schemas.FulfillmentOrder")]
public class FulfillmentOrder
{
    [XmlElement("FulfillmentId")]
    public string FulfillmentId { get; set; } = string.Empty;

    [XmlElement("SourceOrderRef")]
    public string SourceOrderRef { get; set; } = string.Empty;

    [XmlElement("CustomerDetails")]
    public CustomerDetails CustomerDetails { get; set; } = new();

    [XmlElement("RequestedDate")]
    public DateTime RequestedDate { get; set; }

    [XmlElement("LineItems")]
    public FulfillmentLineItems LineItems { get; set; } = new();

    [XmlElement("OrderTotal")]
    public decimal OrderTotal { get; set; }

    [XmlElement("CurrencyCode")]
    public string CurrencyCode { get; set; } = string.Empty;

    [XmlElement("ShipTo")]
    public ShipTo ShipTo { get; set; } = new();

    [XmlElement("Status")]
    public string Status { get; set; } = "PENDING";
}

/// <summary>
/// Customer information in the fulfillment order.
/// </summary>
public class CustomerDetails
{
    [XmlElement("Id")]
    public string Id { get; set; } = string.Empty;

    [XmlElement("Name")]
    public string Name { get; set; } = string.Empty;

    [XmlElement("Email")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Container for line items in the fulfillment order.
/// </summary>
public class FulfillmentLineItems
{
    [XmlElement("LineItem")]
    public List<LineItem> LineItem { get; set; } = new();
}

/// <summary>
/// A single line item in the fulfillment order.
/// </summary>
public class LineItem
{
    [XmlElement("SKU")]
    public string SKU { get; set; } = string.Empty;

    [XmlElement("Description")]
    public string Description { get; set; } = string.Empty;

    [XmlElement("Qty")]
    public int Qty { get; set; }

    [XmlElement("Price")]
    public decimal Price { get; set; }

    [XmlElement("LineTotal")]
    public decimal LineTotal { get; set; }
}

/// <summary>
/// Shipping destination address in the fulfillment order.
/// </summary>
public class ShipTo
{
    [XmlElement("AddressLine1")]
    public string AddressLine1 { get; set; } = string.Empty;

    [XmlElement("City")]
    public string City { get; set; } = string.Empty;

    [XmlElement("StateProvince")]
    public string StateProvince { get; set; } = string.Empty;

    [XmlElement("PostalCode")]
    public string PostalCode { get; set; } = string.Empty;

    [XmlElement("CountryCode")]
    public string CountryCode { get; set; } = string.Empty;
}
