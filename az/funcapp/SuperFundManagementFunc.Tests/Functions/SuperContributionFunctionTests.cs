using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SuperFundManagement.Functions.Functions;
using SuperFundManagement.Functions.Models;
using SuperFundManagement.Functions.Services;
using Xunit;

namespace SuperFundManagement.Functions.Tests.Functions
{
    public class SuperContributionFunctionTests
    {
        private const string ValidXml =
            """
            <?xml version="1.0" encoding="utf-8"?>
            <SuperContributionRequest xmlns="http://SuperFundManagement.Schemas.SuperContribution">
              <ContributionId>C-001</ContributionId>
              <EmployerId>EMP-1</EmployerId>
              <EmployerName>Acme Corp</EmployerName>
              <EmployerABN>51824753556</EmployerABN>
              <PayPeriodEndDate>2024-06-30</PayPeriodEndDate>
              <Members>
                <Member>
                  <MemberAccountNumber>ACC-001</MemberAccountNumber>
                  <MemberName>John Doe</MemberName>
                  <ContributionType>Concessional</ContributionType>
                  <GrossAmount>1000.00</GrossAmount>
                </Member>
              </Members>
              <TotalContribution>1000.00</TotalContribution>
              <Currency>AUD</Currency>
              <PaymentReference>PAY-REF-001</PaymentReference>
            </SuperContributionRequest>
            """;

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

        private static (SuperContributionFunction sut,
                        Mock<IContributionTransformService> transformMock,
                        Mock<IFundAllocationSenderService> senderMock)
            BuildSut(HttpStatusCode senderStatus = HttpStatusCode.OK)
        {
            var transformMock = new Mock<IContributionTransformService>();
            transformMock
                .Setup(s => s.Transform(It.IsAny<SuperContributionRequest>()))
                .Returns(BuildInstruction());

            var senderMock = new Mock<IFundAllocationSenderService>();
            senderMock
                .Setup(s => s.SendAsync(It.IsAny<FundAllocationInstruction>()))
                .ReturnsAsync(new HttpResponseMessage(senderStatus));

            var sut = new SuperContributionFunction(
                transformMock.Object,
                senderMock.Object,
                NullLogger<SuperContributionFunction>.Instance);

            return (sut, transformMock, senderMock);
        }

        private static HttpRequestData BuildRequest(string body, HttpStatusCode? _ = null)
        {
            var context = new Mock<FunctionContext>();
            var request = new Mock<HttpRequestData>(context.Object);
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));
            request.Setup(r => r.Body).Returns(stream);
            request.Setup(r => r.CreateResponse()).Returns(() =>
            {
                var response = new Mock<HttpResponseData>(context.Object);
                response.SetupProperty(r => r.StatusCode);
                var responseBody = new MemoryStream();
                response.Setup(r => r.Body).Returns(responseBody);
                response.Setup(r => r.Headers).Returns(new HttpHeadersCollection());
                return response.Object;
            });
            return request.Object;
        }

        [Fact]
        public async Task RunAsync_ValidXml_Returns202Accepted()
        {
            var (sut, _, _) = BuildSut();
            var req = BuildRequest(ValidXml);

            var result = await sut.RunAsync(req);

            result.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        [Fact]
        public async Task RunAsync_ValidXml_CallsTransformService()
        {
            var (sut, transformMock, _) = BuildSut();
            var req = BuildRequest(ValidXml);

            await sut.RunAsync(req);

            transformMock.Verify(s => s.Transform(It.IsAny<SuperContributionRequest>()), Times.Once);
        }

        [Fact]
        public async Task RunAsync_ValidXml_CallsSenderService()
        {
            var (sut, _, senderMock) = BuildSut();
            var req = BuildRequest(ValidXml);

            await sut.RunAsync(req);

            senderMock.Verify(s => s.SendAsync(It.IsAny<FundAllocationInstruction>()), Times.Once);
        }

        [Fact]
        public async Task RunAsync_MalformedXml_Returns400BadRequest()
        {
            var (sut, _, _) = BuildSut();
            var req = BuildRequest("this is not xml");

            var result = await sut.RunAsync(req);

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task RunAsync_EmptyBody_Returns400BadRequest()
        {
            var (sut, _, _) = BuildSut();
            var req = BuildRequest(string.Empty);

            var result = await sut.RunAsync(req);

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task RunAsync_SenderReturns500_Returns502BadGateway()
        {
            var (sut, _, _) = BuildSut(HttpStatusCode.InternalServerError);
            var req = BuildRequest(ValidXml);

            var result = await sut.RunAsync(req);

            result.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        }

        [Fact]
        public async Task RunAsync_SenderThrows_Returns502BadGateway()
        {
            var transformMock = new Mock<IContributionTransformService>();
            transformMock
                .Setup(s => s.Transform(It.IsAny<SuperContributionRequest>()))
                .Returns(BuildInstruction());

            var senderMock = new Mock<IFundAllocationSenderService>();
            senderMock
                .Setup(s => s.SendAsync(It.IsAny<FundAllocationInstruction>()))
                .ThrowsAsync(new HttpRequestException("Connection refused"));

            var sut = new SuperContributionFunction(
                transformMock.Object,
                senderMock.Object,
                NullLogger<SuperContributionFunction>.Instance);

            var req = BuildRequest(ValidXml);
            var result = await sut.RunAsync(req);

            result.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        }

        [Fact]
        public async Task RunAsync_TransformThrows_Returns400BadRequest()
        {
            var transformMock = new Mock<IContributionTransformService>();
            transformMock
                .Setup(s => s.Transform(It.IsAny<SuperContributionRequest>()))
                .Throws(new ArgumentException("Transform failed"));

            var senderMock = new Mock<IFundAllocationSenderService>();

            var sut = new SuperContributionFunction(
                transformMock.Object,
                senderMock.Object,
                NullLogger<SuperContributionFunction>.Instance);

            var req = BuildRequest(ValidXml);
            var result = await sut.RunAsync(req);

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
