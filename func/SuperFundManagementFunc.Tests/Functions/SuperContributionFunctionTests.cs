using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using SuperFundManagementFunc.Functions;
using SuperFundManagementFunc.Models;
using SuperFundManagementFunc.Services;
using Xunit;

namespace SuperFundManagementFunc.Tests.Functions;

public class SuperContributionFunctionTests
{
    private const string ValidXmlContribution = @"<?xml version=""1.0"" encoding=""utf-8""?>
<SuperContributionRequest xmlns=""http://SuperFundManagement.Schemas.SuperContribution"">
  <ContributionId>CONT-TEST-001</ContributionId>
  <EmployerId>EMP-99</EmployerId>
  <EmployerName>Test Employer Pty Ltd</EmployerName>
  <EmployerABN>12345678901</EmployerABN>
  <PayPeriodEndDate>2024-06-30</PayPeriodEndDate>
  <Members>
    <Member>
      <MemberAccountNumber>SF-200001</MemberAccountNumber>
      <MemberName>Test Member</MemberName>
      <ContributionType>SuperannuationGuarantee</ContributionType>
      <GrossAmount>500.00</GrossAmount>
    </Member>
  </Members>
  <TotalContribution>500.00</TotalContribution>
  <Currency>AUD</Currency>
  <PaymentReference>PAY-REF-TEST</PaymentReference>
</SuperContributionRequest>";

    private static FundAllocation BuildFundAllocation(string contributionId = "CONT-TEST-001") => new()
    {
        AllocationId = "FA-" + contributionId,
        SourceContributionRef = contributionId,
        Status = "PENDING",
        TotalAllocated = 500.00m,
        CurrencyCode = "AUD",
        EmployerDetails = new EmployerDetails { EmployerId = "EMP-99", EmployerName = "Test Employer Pty Ltd", ABN = "12345678901" },
        MemberAllocations = new MemberAllocations { Allocation = new List<MemberAllocation>() }
    };

    private static HttpRequestData CreateRequest(string body)
    {
        var context = new Mock<FunctionContext>();
        return new FakeHttpRequestData(context.Object, new Uri("http://localhost/api/contributions"), body);
    }

    [Fact]
    public async Task ProcessContribution_WithValidXml_Returns202()
    {
        var transformMock = new Mock<IContributionTransformService>();
        var senderMock = new Mock<IFundAllocationSenderService>();
        var allocation = BuildFundAllocation();

        transformMock.Setup(t => t.Transform(It.IsAny<SuperContribution>())).Returns(allocation);
        senderMock.Setup(s => s.SendAsync(allocation))
                  .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Accepted));

        var sut = new SuperContributionFunction(
            transformMock.Object, senderMock.Object,
            Mock.Of<ILogger<SuperContributionFunction>>());

        var response = await sut.ProcessContribution(CreateRequest(ValidXmlContribution));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task ProcessContribution_WithMissingContributionId_Returns400()
    {
        const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<SuperContributionRequest xmlns=""http://SuperFundManagement.Schemas.SuperContribution"">
  <ContributionId></ContributionId>
  <EmployerId>EMP-99</EmployerId>
  <EmployerName>Test</EmployerName>
  <EmployerABN>12345678901</EmployerABN>
  <PayPeriodEndDate>2024-06-30</PayPeriodEndDate>
  <Members><Member><MemberAccountNumber>SF-001</MemberAccountNumber><MemberName>A</MemberName><ContributionType>SuperannuationGuarantee</ContributionType><GrossAmount>100</GrossAmount></Member></Members>
  <TotalContribution>100</TotalContribution>
  <Currency>AUD</Currency>
  <PaymentReference>REF-001</PaymentReference>
</SuperContributionRequest>";

        var sut = new SuperContributionFunction(
            Mock.Of<IContributionTransformService>(),
            Mock.Of<IFundAllocationSenderService>(),
            Mock.Of<ILogger<SuperContributionFunction>>());

        var response = await sut.ProcessContribution(CreateRequest(xml));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProcessContribution_WithEmptyMembers_Returns400()
    {
        const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<SuperContributionRequest xmlns=""http://SuperFundManagement.Schemas.SuperContribution"">
  <ContributionId>CONT-001</ContributionId>
  <EmployerId>EMP-99</EmployerId>
  <EmployerName>Test</EmployerName>
  <EmployerABN>12345678901</EmployerABN>
  <PayPeriodEndDate>2024-06-30</PayPeriodEndDate>
  <Members></Members>
  <TotalContribution>0</TotalContribution>
  <Currency>AUD</Currency>
  <PaymentReference>REF-001</PaymentReference>
</SuperContributionRequest>";

        var sut = new SuperContributionFunction(
            Mock.Of<IContributionTransformService>(),
            Mock.Of<IFundAllocationSenderService>(),
            Mock.Of<ILogger<SuperContributionFunction>>());

        var response = await sut.ProcessContribution(CreateRequest(xml));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProcessContribution_WhenPlatformFails_Returns502()
    {
        var transformMock = new Mock<IContributionTransformService>();
        var senderMock = new Mock<IFundAllocationSenderService>();
        var allocation = BuildFundAllocation();

        transformMock.Setup(t => t.Transform(It.IsAny<SuperContribution>())).Returns(allocation);
        senderMock.Setup(s => s.SendAsync(allocation))
                  .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var sut = new SuperContributionFunction(
            transformMock.Object, senderMock.Object,
            Mock.Of<ILogger<SuperContributionFunction>>());

        var response = await sut.ProcessContribution(CreateRequest(ValidXmlContribution));

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task ProcessContribution_WhenPlatformThrows_Returns502()
    {
        var transformMock = new Mock<IContributionTransformService>();
        var senderMock = new Mock<IFundAllocationSenderService>();
        var allocation = BuildFundAllocation();

        transformMock.Setup(t => t.Transform(It.IsAny<SuperContribution>())).Returns(allocation);
        senderMock.Setup(s => s.SendAsync(allocation))
                  .ThrowsAsync(new HttpRequestException("Connection refused"));

        var sut = new SuperContributionFunction(
            transformMock.Object, senderMock.Object,
            Mock.Of<ILogger<SuperContributionFunction>>());

        var response = await sut.ProcessContribution(CreateRequest(ValidXmlContribution));

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
