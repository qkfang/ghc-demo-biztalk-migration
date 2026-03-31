using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SuperFundManagement.Functions;
using Xunit;

namespace SuperFundManagement.Functions.Tests;

public class ContributionTransformServiceTests
{
    private readonly ContributionTransformService _service;

    public ContributionTransformServiceTests()
    {
        _service = new ContributionTransformService(NullLogger<ContributionTransformService>.Instance);
    }

    private static SuperContributionRequest BuildValidRequest(string contributionId = "C001", string abn = "51824753556") =>
        new SuperContributionRequest
        {
            ContributionId = contributionId,
            EmployerId = "EMP001",
            EmployerName = "Acme Corp",
            EmployerABN = abn,
            PayPeriodEndDate = "2024-06-30",
            TotalContribution = 1000m,
            Currency = "AUD",
            PaymentReference = "REF001",
            Members = new Members
            {
                Member = new List<Member>
                {
                    new Member
                    {
                        MemberAccountNumber = "ACC001",
                        MemberName = "John Smith",
                        ContributionType = "SG",
                        GrossAmount = 1000m
                    }
                }
            }
        };

    [Fact]
    public void Transform_ValidRequest_ReturnsCorrectAllocationId()
    {
        var request = BuildValidRequest("C001");
        var result = _service.Transform(request);
        result.AllocationId.Should().StartWith("FA-");
        result.AllocationId.Should().Contain("C001");
        result.AllocationId.Should().Be("FA-C001");
    }

    [Fact]
    public void Transform_ValidRequest_FormatsABN()
    {
        var request = BuildValidRequest(abn: "51824753556");
        var result = _service.Transform(request);
        result.EmployerDetails.ABN.Should().Be("51 824 753 556");
    }

    [Fact]
    public void Transform_ValidRequest_CalculatesNetContribution()
    {
        var request = BuildValidRequest();
        request.Members.Member[0].GrossAmount = 1000m;
        var result = _service.Transform(request);
        result.MemberAllocations.Allocation[0].ContributionAmount.Should().Be(850.00m);
    }

    [Fact]
    public void Transform_ValidRequest_SetsStatusToPending()
    {
        var request = BuildValidRequest();
        var result = _service.Transform(request);
        result.Status.Should().Be("PENDING");
        result.MemberAllocations.Allocation[0].AllocationStatus.Should().Be("PENDING");
    }

    [Fact]
    public void Transform_NullRequest_ThrowsArgumentNullException()
    {
        Action act = () => _service.Transform(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
