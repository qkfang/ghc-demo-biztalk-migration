using System.Net;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SuperFundManagement.Functions.Functions;
using SuperFundManagement.Functions.Models;
using SuperFundManagement.Functions.Services;
using Xunit;

namespace SuperFundManagement.Functions.Tests.Functions;

// ─── HttpRequestData / HttpResponseData test doubles ─────────────────────────

public class MockHttpRequestData : HttpRequestData
{
    private readonly MemoryStream _body;

    public MockHttpRequestData(FunctionContext functionContext, string body)
        : base(functionContext)
    {
        _body   = new MemoryStream(Encoding.UTF8.GetBytes(body));
        Headers = new HttpHeadersCollection();
    }

    public override Stream Body => _body;
    public override HttpHeadersCollection Headers { get; }
    public override IReadOnlyCollection<IHttpCookie> Cookies => new List<IHttpCookie>();
    public override Uri Url => new Uri("http://localhost/api/SuperFundManagement/Receive");
    public override IEnumerable<ClaimsIdentity> Identities => new List<ClaimsIdentity>();
    public override string Method => "POST";
    public override HttpResponseData CreateResponse() => new MockHttpResponseData(FunctionContext);
}

public class MockHttpResponseData : HttpResponseData
{
    public MockHttpResponseData(FunctionContext functionContext) : base(functionContext)
    {
        Body    = new MemoryStream();
        Headers = new HttpHeadersCollection();
    }

    public override HttpStatusCode StatusCode { get; set; }
    public override HttpHeadersCollection Headers { get; set; }
    public override Stream Body { get; set; }
    public override HttpCookies Cookies => throw new NotImplementedException();
}

// ─── Tests ────────────────────────────────────────────────────────────────────

public class SuperContributionFunctionTests
{
    private static FunctionContext CreateFunctionContext()
    {
        var serviceCollection = new ServiceCollection();
        var serviceProvider   = serviceCollection.BuildServiceProvider();
        var mockContext       = new Mock<FunctionContext>();
        mockContext.Setup(c => c.InstanceServices).Returns(serviceProvider);
        return mockContext.Object;
    }

    private const string ValidXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<SuperContributionRequest xmlns=""http://SuperFundManagement.Schemas.SuperContribution"">
  <ContributionId>CONT-2024-001</ContributionId>
  <EmployerId>EMP-001</EmployerId>
  <EmployerName>Acme Corp</EmployerName>
  <EmployerABN>51824753556</EmployerABN>
  <PayPeriodEndDate>2024-03-31</PayPeriodEndDate>
  <Members>
    <Member>
      <MemberAccountNumber>ACC-001</MemberAccountNumber>
      <MemberName>Alice Smith</MemberName>
      <ContributionType>SG</ContributionType>
      <GrossAmount>875.00</GrossAmount>
    </Member>
  </Members>
  <TotalContribution>875.00</TotalContribution>
  <Currency>AUD</Currency>
  <PaymentReference>PAY-REF-001</PaymentReference>
</SuperContributionRequest>";

    [Fact]
    public async Task Run_HappyPath_Returns202()
    {
        // Arrange
        var mockTransform = new Mock<IContributionTransformService>();
        mockTransform.Setup(s => s.Transform(It.IsAny<SuperContributionRequest>()))
            .Returns(new FundAllocationInstruction
            {
                AllocationId      = "FA-CONT-2024-001",
                Status            = "PENDING",
                TotalAllocated    = 743.75m,
                MemberAllocations = new MemberAllocationsCollection
                {
                    Allocation = new List<Allocation>
                    {
                        new() { AccountNumber = "ACC-001", MemberName = "Alice Smith", ContributionType = "SG", ContributionAmount = 743.75m, AllocationStatus = "PENDING" }
                    }
                }
            });

        var mockSender = new Mock<IFundAllocationSenderService>();
        mockSender.Setup(s => s.SendAsync(It.IsAny<FundAllocationInstruction>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var sut = new SuperContributionFunction(
            mockTransform.Object,
            mockSender.Object,
            NullLogger<SuperContributionFunction>.Instance);

        var ctx     = CreateFunctionContext();
        var request = new MockHttpRequestData(ctx, ValidXml);

        // Act
        var response = await sut.Run(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Run_BadXml_Returns400()
    {
        // Arrange
        var sut = new SuperContributionFunction(
            Mock.Of<IContributionTransformService>(),
            Mock.Of<IFundAllocationSenderService>(),
            NullLogger<SuperContributionFunction>.Instance);

        var ctx     = CreateFunctionContext();
        var request = new MockHttpRequestData(ctx, "this is not xml at all");

        // Act
        var response = await sut.Run(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Run_SenderFailure_Returns502()
    {
        // Arrange
        var mockTransform = new Mock<IContributionTransformService>();
        mockTransform.Setup(s => s.Transform(It.IsAny<SuperContributionRequest>()))
            .Returns(new FundAllocationInstruction
            {
                AllocationId      = "FA-CONT-2024-001",
                Status            = "PENDING",
                MemberAllocations = new MemberAllocationsCollection { Allocation = new List<Allocation>() }
            });

        var mockSender = new Mock<IFundAllocationSenderService>();
        mockSender.Setup(s => s.SendAsync(It.IsAny<FundAllocationInstruction>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var sut = new SuperContributionFunction(
            mockTransform.Object,
            mockSender.Object,
            NullLogger<SuperContributionFunction>.Instance);

        var ctx     = CreateFunctionContext();
        var request = new MockHttpRequestData(ctx, ValidXml);

        // Act
        var response = await sut.Run(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }
}
