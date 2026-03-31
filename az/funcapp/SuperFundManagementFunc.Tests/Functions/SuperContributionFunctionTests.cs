using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SuperFundManagement.Functions;
using Xunit;

namespace SuperFundManagement.Functions.Tests;

/// <summary>
/// Concrete implementation of HttpRequestData for testing purposes.
/// </summary>
public class TestHttpRequestData : HttpRequestData
{
    private readonly Stream _body;

    public TestHttpRequestData(FunctionContext context, Stream body)
        : base(context)
    {
        _body = body;
    }

    public override Stream Body => _body;
    public override HttpHeadersCollection Headers { get; } = new HttpHeadersCollection();
    public override IReadOnlyCollection<IHttpCookie> Cookies { get; } = new List<IHttpCookie>();
    public override Uri Url { get; } = new Uri("http://localhost/api/supercontribution");
    public override IEnumerable<ClaimsIdentity> Identities { get; } = new List<ClaimsIdentity>();
    public override string Method { get; } = "POST";

    public override HttpResponseData CreateResponse()
    {
        return new TestHttpResponseData(FunctionContext);
    }
}

/// <summary>
/// Concrete implementation of HttpResponseData for testing purposes.
/// </summary>
public class TestHttpResponseData : HttpResponseData
{
    public TestHttpResponseData(FunctionContext context) : base(context)
    {
        Body = new MemoryStream();
        Headers = new HttpHeadersCollection();
    }

    public override HttpStatusCode StatusCode { get; set; }
    public override HttpHeadersCollection Headers { get; set; }
    public override Stream Body { get; set; }
    public override HttpCookies Cookies { get; } = null!;
}

public class SuperContributionFunctionTests
{
    private static readonly string ValidXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<SuperContributionRequest xmlns=""http://SuperFundManagement.Schemas.SuperContribution"">
  <ContributionId>C001</ContributionId>
  <EmployerId>EMP001</EmployerId>
  <EmployerName>Acme Corp</EmployerName>
  <EmployerABN>51824753556</EmployerABN>
  <PayPeriodEndDate>2024-06-30</PayPeriodEndDate>
  <Members>
    <Member>
      <MemberAccountNumber>ACC001</MemberAccountNumber>
      <MemberName>John Smith</MemberName>
      <ContributionType>SG</ContributionType>
      <GrossAmount>1000.00</GrossAmount>
    </Member>
  </Members>
  <TotalContribution>1000.00</TotalContribution>
  <Currency>AUD</Currency>
  <PaymentReference>REF001</PaymentReference>
</SuperContributionRequest>";

    private static FunctionContext CreateFunctionContext()
    {
        var mockContext = new Mock<FunctionContext>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockContext.Setup(c => c.InstanceServices).Returns(mockServiceProvider.Object);
        return mockContext.Object;
    }

    private static HttpRequestData CreateRequest(FunctionContext context, string body)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));
        return new TestHttpRequestData(context, stream);
    }

    private static SuperContributionFunction CreateFunction(
        IContributionTransformService? transformService = null,
        IFundAllocationSenderService? senderService = null)
    {
        transformService ??= new ContributionTransformService(NullLogger<ContributionTransformService>.Instance);
        senderService ??= Mock.Of<IFundAllocationSenderService>();
        return new SuperContributionFunction(transformService, senderService, NullLogger<SuperContributionFunction>.Instance);
    }

    [Fact]
    public async Task Run_ValidXmlBody_Returns202()
    {
        var senderMock = new Mock<IFundAllocationSenderService>();
        senderMock.Setup(s => s.SendAsync(It.IsAny<FundAllocationInstruction>())).Returns(Task.CompletedTask);

        var function = CreateFunction(senderService: senderMock.Object);
        var context = CreateFunctionContext();
        var req = CreateRequest(context, ValidXml);

        var response = await function.Run(req);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Run_InvalidXmlBody_Returns400()
    {
        var function = CreateFunction();
        var context = CreateFunctionContext();
        var req = CreateRequest(context, "<invalid>xml without proper structure</WRONG>");

        var response = await function.Run(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Run_DownstreamFailure_Returns502()
    {
        var senderMock = new Mock<IFundAllocationSenderService>();
        senderMock
            .Setup(s => s.SendAsync(It.IsAny<FundAllocationInstruction>()))
            .ThrowsAsync(new HttpRequestException("Downstream error"));

        var function = CreateFunction(senderService: senderMock.Object);
        var context = CreateFunctionContext();
        var req = CreateRequest(context, ValidXml);

        var response = await function.Run(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task Run_EmptyBody_Returns400()
    {
        var function = CreateFunction();
        var context = CreateFunctionContext();
        var req = CreateRequest(context, string.Empty);

        var response = await function.Run(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
