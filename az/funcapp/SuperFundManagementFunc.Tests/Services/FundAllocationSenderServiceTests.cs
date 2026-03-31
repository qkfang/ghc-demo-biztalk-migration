using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using SuperFundManagement.Functions;
using Xunit;

namespace SuperFundManagement.Functions.Tests;

public class FundAllocationSenderServiceTests
{
    private static FundAllocationInstruction BuildInstruction() => new FundAllocationInstruction
    {
        AllocationId = "FA-C001",
        SourceContributionRef = "C001",
        EmployerDetails = new EmployerDetails { EmployerId = "EMP001", EmployerName = "Acme Corp", ABN = "51 824 753 556" },
        AllocationDate = "2024-06-30",
        TotalAllocated = 850m,
        CurrencyCode = "AUD",
        Status = "PENDING",
        MemberAllocations = new MemberAllocations
        {
            Allocation = new System.Collections.Generic.List<Allocation>
            {
                new Allocation
                {
                    AccountNumber = "ACC001",
                    MemberName = "John Smith",
                    ContributionType = "SG",
                    ContributionAmount = 850m,
                    AllocationStatus = "PENDING"
                }
            }
        }
    };

    private static (FundAllocationSenderService service, Mock<HttpMessageHandler> handlerMock) BuildService(HttpStatusCode statusCode, string responseContent = "OK")
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseContent)
            });

        var httpClient = new HttpClient(handlerMock.Object);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new System.Collections.Generic.KeyValuePair<string, string?>("FundAdminApiUrl", "http://localhost:5050") })
            .Build();

        var service = new FundAllocationSenderService(httpClient, config, NullLogger<FundAllocationSenderService>.Instance);
        return (service, handlerMock);
    }

    [Fact]
    public async Task SendAsync_ValidAllocation_ReturnsSuccess()
    {
        var (service, _) = BuildService(HttpStatusCode.OK);
        var act = async () => await service.SendAsync(BuildInstruction());
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendAsync_DownstreamReturnsError_ThrowsHttpRequestException()
    {
        var (service, _) = BuildService(HttpStatusCode.InternalServerError, "Internal Server Error");
        var act = async () => await service.SendAsync(BuildInstruction());
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task SendAsync_SerializesXmlCorrectly()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        HttpRequestMessage? capturedRequest = null;

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("OK") });

        var httpClient = new HttpClient(handlerMock.Object);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new System.Collections.Generic.KeyValuePair<string, string?>("FundAdminApiUrl", "http://localhost:5050") })
            .Build();

        var service = new FundAllocationSenderService(httpClient, config, NullLogger<FundAllocationSenderService>.Instance);
        await service.SendAsync(BuildInstruction());

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Content.Should().NotBeNull();
        var mediaType = capturedRequest.Content!.Headers.ContentType?.MediaType;
        mediaType.Should().Be("application/xml");
    }
}
