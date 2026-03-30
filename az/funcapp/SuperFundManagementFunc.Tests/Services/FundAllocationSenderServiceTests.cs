using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using SuperFundManagement.Functions.Models;
using SuperFundManagement.Functions.Services;
using Xunit;

namespace SuperFundManagement.Functions.Tests.Services
{
    public class FundAllocationSenderServiceTests
    {
        private static FundAllocationSenderService BuildSut(HttpStatusCode statusCode,
            out Mock<HttpMessageHandler> handlerMock)
        {
            handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(statusCode));

            var client = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:5050/api/allocations")
            };

            return new FundAllocationSenderService(client,
                NullLogger<FundAllocationSenderService>.Instance);
        }

        private static FundAllocationInstruction BuildInstruction() => new()
        {
            AllocationId          = "FA-C-001",
            SourceContributionRef = "C-001",
            EmployerDetails = new EmployerDetails
            {
                EmployerId   = "EMP-1",
                EmployerName = "Acme Corp",
                ABN          = "51 824 753 556"
            },
            AllocationDate = new DateTime(2024, 6, 30),
            MemberAllocations = new MemberAllocationsWrapper
            {
                Allocation =
                [
                    new Allocation
                    {
                        AccountNumber      = "ACC-001",
                        MemberName         = "John Doe",
                        ContributionType   = "Concessional",
                        ContributionAmount = 850m,
                        AllocationStatus   = "PENDING"
                    }
                ]
            },
            TotalAllocated = 850m,
            CurrencyCode   = "AUD",
            Status         = "PENDING"
        };

        [Fact]
        public async Task SendAsync_ValidInstruction_ReturnsSuccessResponse()
        {
            var sut = BuildSut(HttpStatusCode.OK, out _);
            var instruction = BuildInstruction();

            var response = await sut.SendAsync(instruction);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task SendAsync_ValidInstruction_PostsXmlToApi()
        {
            var sut = BuildSut(HttpStatusCode.OK, out var handlerMock);
            var instruction = BuildInstruction();

            await sut.SendAsync(instruction);

            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post &&
                    r.Content!.Headers.ContentType!.MediaType == "application/xml"),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task SendAsync_DownstreamReturns500_ReturnsNonSuccessResponse()
        {
            var sut = BuildSut(HttpStatusCode.InternalServerError, out _);
            var instruction = BuildInstruction();

            var response = await sut.SendAsync(instruction);

            response.IsSuccessStatusCode.Should().BeFalse();
        }

        [Fact]
        public async Task SendAsync_HttpClientThrows_PropagatesException()
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Connection refused"));

            var client = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:5050/api/allocations")
            };
            var sut = new FundAllocationSenderService(client,
                NullLogger<FundAllocationSenderService>.Instance);

            var act = () => sut.SendAsync(BuildInstruction());

            await act.Should().ThrowAsync<HttpRequestException>();
        }
    }
}
