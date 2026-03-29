using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using SuperFundManagementFunc.Models;
using SuperFundManagementFunc.Services;
using Xunit;

namespace SuperFundManagementFunc.Tests.Services;

public class FundAllocationSenderServiceTests
{
    private static FundAllocation BuildSampleFundAllocation() => new()
    {
        AllocationId = "FA-CONT-2024-001",
        SourceContributionRef = "CONT-2024-001",
        Status = "PENDING",
        TotalAllocated = 875.00m,
        CurrencyCode = "AUD",
        AllocationDate = new DateTime(2024, 6, 30, 0, 0, 0, DateTimeKind.Utc),
        EmployerDetails = new EmployerDetails { EmployerId = "EMP-001", EmployerName = "Acme Corporation Pty Ltd", ABN = "51824753556" },
        MemberAllocations = new MemberAllocations
        {
            Allocation = new List<MemberAllocation>
            {
                new() { AccountNumber = "SF-100001", MemberName = "Jane Smith", ContributionType = "SuperannuationGuarantee", ContributionAmount = 875.00m, AllocationStatus = "PENDING" }
            }
        }
    };

    private static (FundAllocationSenderService sut, Mock<HttpMessageHandler> handlerMock)
        BuildSut(HttpResponseMessage httpResponse, string serviceUrl = "https://fund-admin.example.com/api/allocations")
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
            .AddInMemoryCollection(new Dictionary<string, string?> { ["FundAdminPlatformUrl"] = serviceUrl })
            .Build();

        var loggerMock = new Mock<ILogger<FundAllocationSenderService>>();

        var sut = new FundAllocationSenderService(factoryMock.Object, config, loggerMock.Object);
        return (sut, handlerMock);
    }

    [Fact]
    public async Task SendAsync_PostsToCorrectUrl()
    {
        const string expectedUrl = "https://fund-admin.example.com/api/allocations";
        var (sut, handlerMock) = BuildSut(new HttpResponseMessage(HttpStatusCode.Accepted), expectedUrl);

        await sut.SendAsync(BuildSampleFundAllocation());

        handlerMock.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Post &&
                r.RequestUri!.ToString() == expectedUrl),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_SerializesAllocationAsXml()
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
                capturedBody = await req.Content!.ReadAsStringAsync();
                capturedContentType = req.Content.Headers.ContentType?.MediaType;
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Accepted));

        var httpClient = new HttpClient(handlerMock.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["FundAdminPlatformUrl"] = "https://test.example.com" })
            .Build();
        var sut = new FundAllocationSenderService(factoryMock.Object, config, Mock.Of<ILogger<FundAllocationSenderService>>());

        await sut.SendAsync(BuildSampleFundAllocation());

        Assert.NotNull(capturedBody);
        Assert.Contains("FundAllocationInstruction", capturedBody);
        Assert.Contains("FA-CONT-2024-001", capturedBody);
        Assert.Equal("application/xml", capturedContentType);
    }

    [Fact]
    public async Task SendAsync_ReturnsSuccessResponse()
    {
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.Accepted);
        var (sut, _) = BuildSut(expectedResponse);

        var result = await sut.SendAsync(BuildSampleFundAllocation());

        Assert.True(result.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Accepted, result.StatusCode);
    }

    [Fact]
    public async Task SendAsync_ThrowsArgumentNullException_WhenAllocationIsNull()
    {
        var (sut, _) = BuildSut(new HttpResponseMessage(HttpStatusCode.OK));

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.SendAsync(null!));
    }
}
