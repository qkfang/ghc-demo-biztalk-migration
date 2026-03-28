using System.Net;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using OrderProcessingFunc.Models;
using OrderProcessingFunc.Services;
using Xunit;

namespace OrderProcessingFunc.Tests.Services;

public class FulfillmentSenderServiceTests
{
    private static FulfillmentOrder BuildSampleFulfillmentOrder() => new()
    {
        FulfillmentId = "FF-ORD-001",
        SourceOrderRef = "ORD-001",
        Status = "PENDING",
        OrderTotal = 149.97m,
        CurrencyCode = "USD",
        RequestedDate = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc),
        CustomerDetails = new CustomerDetails { Id = "CUST-42", Name = "Jane Smith", Email = "jane@example.com" },
        LineItems = new FulfillmentLineItems
        {
            LineItem = new List<LineItem>
            {
                new() { SKU = "SKU-A", Description = "Widget Pro", Qty = 3, Price = 29.99m, LineTotal = 89.97m }
            }
        },
        ShipTo = new ShipTo
        {
            AddressLine1 = "123 Main St", City = "Springfield",
            StateProvince = "IL", PostalCode = "62701", CountryCode = "US"
        }
    };

    private static (FulfillmentSenderService sut, Mock<HttpMessageHandler> handlerMock)
        BuildSut(HttpResponseMessage httpResponse, string serviceUrl = "https://fulfillment.example.com/api/fulfillment")
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var httpClient = new HttpClient(handlerMock.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["FulfillmentServiceUrl"] = serviceUrl })
            .Build();

        var loggerMock = new Mock<ILogger<FulfillmentSenderService>>();

        var sut = new FulfillmentSenderService(factoryMock.Object, config, loggerMock.Object);
        return (sut, handlerMock);
    }

    [Fact]
    public async Task SendAsync_PostsToCorrectUrl()
    {
        const string expectedUrl = "https://fulfillment.example.com/api/fulfillment";
        var (sut, handlerMock) = BuildSut(new HttpResponseMessage(HttpStatusCode.Accepted), expectedUrl);

        await sut.SendAsync(BuildSampleFulfillmentOrder());

        handlerMock.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Post &&
                r.RequestUri!.ToString() == expectedUrl),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_SerializesOrderAsXml()
    {
        string? capturedBody = null;
        string? capturedContentType = null;
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                // Read content before HttpClient disposes it
                capturedBody = await req.Content!.ReadAsStringAsync();
                capturedContentType = req.Content.Headers.ContentType?.MediaType;
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Accepted));

        var httpClient = new HttpClient(handlerMock.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["FulfillmentServiceUrl"] = "https://test.example.com" })
            .Build();
        var sut = new FulfillmentSenderService(factoryMock.Object, config, Mock.Of<ILogger<FulfillmentSenderService>>());

        await sut.SendAsync(BuildSampleFulfillmentOrder());

        Assert.NotNull(capturedBody);
        Assert.Contains("FulfillmentOrder", capturedBody);
        Assert.Contains("FF-ORD-001", capturedBody);
        Assert.Equal("application/xml", capturedContentType);
    }

    [Fact]
    public async Task SendAsync_ReturnsSuccessResponse()
    {
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.Accepted);
        var (sut, _) = BuildSut(expectedResponse);

        var result = await sut.SendAsync(BuildSampleFulfillmentOrder());

        Assert.True(result.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Accepted, result.StatusCode);
    }

    [Fact]
    public async Task SendAsync_ThrowsArgumentNullException_WhenOrderIsNull()
    {
        var (sut, _) = BuildSut(new HttpResponseMessage(HttpStatusCode.OK));

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.SendAsync(null!));
    }
}
