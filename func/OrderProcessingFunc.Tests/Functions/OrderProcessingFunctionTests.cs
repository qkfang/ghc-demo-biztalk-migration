using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using OrderProcessingFunc.Functions;
using OrderProcessingFunc.Models;
using OrderProcessingFunc.Services;
using Xunit;

namespace OrderProcessingFunc.Tests.Functions;

public class OrderProcessingFunctionTests
{
    private const string ValidXmlOrder = @"<?xml version=""1.0"" encoding=""utf-8""?>
<OrderRequest xmlns=""http://OrderProcessing.Schemas.SourceOrder"">
  <OrderId>ORD-TEST-001</OrderId>
  <CustomerId>CUST-99</CustomerId>
  <CustomerName>Test User</CustomerName>
  <CustomerEmail>test@example.com</CustomerEmail>
  <OrderDate>2024-06-15T10:30:00Z</OrderDate>
  <Items>
    <Item>
      <ProductCode>SKU-X</ProductCode>
      <ProductName>Test Widget</ProductName>
      <Quantity>2</Quantity>
      <UnitPrice>19.99</UnitPrice>
    </Item>
  </Items>
  <TotalAmount>39.98</TotalAmount>
  <Currency>USD</Currency>
  <ShippingAddress>
    <Street>456 Oak Ave</Street>
    <City>Shelbyville</City>
    <State>TN</State>
    <ZipCode>37160</ZipCode>
    <Country>US</Country>
  </ShippingAddress>
</OrderRequest>";

    private static FulfillmentOrder BuildFulfillmentOrder(string orderId = "ORD-TEST-001") => new()
    {
        FulfillmentId = "FF-" + orderId,
        SourceOrderRef = orderId,
        Status = "PENDING",
        OrderTotal = 39.98m,
        CurrencyCode = "USD",
        CustomerDetails = new CustomerDetails { Id = "CUST-99", Name = "Test User", Email = "test@example.com" },
        LineItems = new FulfillmentLineItems { LineItem = new List<LineItem>() },
        ShipTo = new ShipTo()
    };

    /// <summary>
    /// Creates a testable HttpRequestData with the given body string.
    /// Uses a concrete FakeHttpRequestData since HttpRequestData is abstract.
    /// </summary>
    private static HttpRequestData CreateRequest(string body)
    {
        var context = new Mock<FunctionContext>();
        return new FakeHttpRequestData(context.Object, new Uri("http://localhost/api/orders"), body);
    }

    [Fact]
    public async Task ProcessOrder_WithValidXml_Returns202()
    {
        var transformMock = new Mock<IOrderTransformService>();
        var senderMock = new Mock<IFulfillmentSenderService>();
        var fulfillment = BuildFulfillmentOrder();

        transformMock.Setup(t => t.Transform(It.IsAny<SourceOrder>())).Returns(fulfillment);
        senderMock.Setup(s => s.SendAsync(fulfillment))
                  .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Accepted));

        var sut = new OrderProcessingFunction(
            transformMock.Object, senderMock.Object,
            Mock.Of<ILogger<OrderProcessingFunction>>());

        var response = await sut.ProcessOrder(CreateRequest(ValidXmlOrder));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task ProcessOrder_WithMissingOrderId_Returns400()
    {
        const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<OrderRequest xmlns=""http://OrderProcessing.Schemas.SourceOrder"">
  <OrderId></OrderId>
  <CustomerId>CUST-99</CustomerId>
  <CustomerName>Test</CustomerName>
  <CustomerEmail>test@example.com</CustomerEmail>
  <OrderDate>2024-06-15T10:30:00Z</OrderDate>
  <Items><Item><ProductCode>X</ProductCode><ProductName>Y</ProductName><Quantity>1</Quantity><UnitPrice>1</UnitPrice></Item></Items>
  <TotalAmount>1</TotalAmount>
  <Currency>USD</Currency>
  <ShippingAddress><Street>1</Street><City>A</City><State>B</State><ZipCode>C</ZipCode><Country>D</Country></ShippingAddress>
</OrderRequest>";

        var sut = new OrderProcessingFunction(
            Mock.Of<IOrderTransformService>(),
            Mock.Of<IFulfillmentSenderService>(),
            Mock.Of<ILogger<OrderProcessingFunction>>());

        var response = await sut.ProcessOrder(CreateRequest(xml));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProcessOrder_WithEmptyItems_Returns400()
    {
        const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<OrderRequest xmlns=""http://OrderProcessing.Schemas.SourceOrder"">
  <OrderId>ORD-001</OrderId>
  <CustomerId>CUST-99</CustomerId>
  <CustomerName>Test</CustomerName>
  <CustomerEmail>test@example.com</CustomerEmail>
  <OrderDate>2024-06-15T10:30:00Z</OrderDate>
  <Items></Items>
  <TotalAmount>0</TotalAmount>
  <Currency>USD</Currency>
  <ShippingAddress><Street>1</Street><City>A</City><State>B</State><ZipCode>C</ZipCode><Country>D</Country></ShippingAddress>
</OrderRequest>";

        var sut = new OrderProcessingFunction(
            Mock.Of<IOrderTransformService>(),
            Mock.Of<IFulfillmentSenderService>(),
            Mock.Of<ILogger<OrderProcessingFunction>>());

        var response = await sut.ProcessOrder(CreateRequest(xml));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProcessOrder_WhenDownstreamFails_Returns502()
    {
        var transformMock = new Mock<IOrderTransformService>();
        var senderMock = new Mock<IFulfillmentSenderService>();
        var fulfillment = BuildFulfillmentOrder();

        transformMock.Setup(t => t.Transform(It.IsAny<SourceOrder>())).Returns(fulfillment);
        senderMock.Setup(s => s.SendAsync(fulfillment))
                  .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var sut = new OrderProcessingFunction(
            transformMock.Object, senderMock.Object,
            Mock.Of<ILogger<OrderProcessingFunction>>());

        var response = await sut.ProcessOrder(CreateRequest(ValidXmlOrder));

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task ProcessOrder_WhenDownstreamThrows_Returns502()
    {
        var transformMock = new Mock<IOrderTransformService>();
        var senderMock = new Mock<IFulfillmentSenderService>();
        var fulfillment = BuildFulfillmentOrder();

        transformMock.Setup(t => t.Transform(It.IsAny<SourceOrder>())).Returns(fulfillment);
        senderMock.Setup(s => s.SendAsync(fulfillment))
                  .ThrowsAsync(new HttpRequestException("Connection refused"));

        var sut = new OrderProcessingFunction(
            transformMock.Object, senderMock.Object,
            Mock.Of<ILogger<OrderProcessingFunction>>());

        var response = await sut.ProcessOrder(CreateRequest(ValidXmlOrder));

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }
}

/// <summary>
/// Minimal concrete implementation of HttpRequestData for unit testing.
/// </summary>
internal sealed class FakeHttpRequestData : HttpRequestData
{
    private readonly Stream _body;

    public FakeHttpRequestData(FunctionContext ctx, Uri url, string body)
        : base(ctx)
    {
        Url = url;
        _body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        Headers = new HttpHeadersCollection();
        Headers.Add("Content-Type", "application/xml");
    }

    public override Stream Body => _body;
    public override HttpHeadersCollection Headers { get; }
    public override IReadOnlyCollection<IHttpCookie> Cookies => Array.Empty<IHttpCookie>();
    public override Uri Url { get; }
    public override IEnumerable<ClaimsIdentity> Identities => Array.Empty<ClaimsIdentity>();
    public override string Method => "POST";

    public override HttpResponseData CreateResponse()
        => new FakeHttpResponseData(FunctionContext);
}

internal sealed class FakeHttpResponseData : HttpResponseData
{
    public FakeHttpResponseData(FunctionContext ctx) : base(ctx) { }

    public override HttpStatusCode StatusCode { get; set; }
    public override HttpHeadersCollection Headers { get; set; } = new();
    public override Stream Body { get; set; } = new MemoryStream();
    public override HttpCookies Cookies => throw new NotImplementedException();
}
