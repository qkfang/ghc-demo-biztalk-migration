using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using SuperFundManagement.Functions.Models;
using SuperFundManagement.Functions.Services;
using Xunit;

namespace SuperFundManagement.Functions.Tests.Services;

public class FundAllocationSenderServiceTests
{
    private static FundAllocationInstruction BuildInstruction() => new()
    {
        AllocationId          = "FA-CONT-001",
        SourceContributionRef = "CONT-001",
        EmployerDetails       = new EmployerDetails { EmployerId = "EMP-001", EmployerName = "Acme", ABN = "51 824 753 556" },
        AllocationDate        = new DateTime(2024, 3, 31),
        MemberAllocations     = new MemberAllocationsCollection
        {
            Allocation = new List<Allocation>
            {
                new() { AccountNumber = "ACC-001", MemberName = "Alice", ContributionType = "SG", ContributionAmount = 743.75m, AllocationStatus = "PENDING" }
            }
        },
        TotalAllocated = 743.75m,
        CurrencyCode   = "AUD",
        Status         = "PENDING"
    };

    [Fact]
    public async Task SendAsync_HappyPath_CallsHttpClient()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:5050/api/allocations")
        };
        var sut = new FundAllocationSenderService(httpClient, NullLogger<FundAllocationSenderService>.Instance);

        // Act
        var result = await sut.SendAsync(BuildInstruction());

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        handlerMock.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_SerializesToXml_ContentTypeIsApplicationXml()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:5050/api/allocations")
        };
        var sut = new FundAllocationSenderService(httpClient, NullLogger<FundAllocationSenderService>.Instance);

        // Act
        await sut.SendAsync(BuildInstruction());

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Content!.Headers.ContentType!.MediaType.Should().Be("application/xml");

        var body = await capturedRequest.Content.ReadAsStringAsync();
        body.Should().Contain("FundAllocationInstruction");
        body.Should().Contain("FA-CONT-001");
    }
}
