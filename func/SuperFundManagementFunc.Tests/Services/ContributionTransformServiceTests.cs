using SuperFundManagementFunc.Models;
using SuperFundManagementFunc.Services;
using Xunit;

namespace SuperFundManagementFunc.Tests.Services;

public class ContributionTransformServiceTests
{
    private readonly ContributionTransformService _sut = new();

    private static SuperContribution BuildSampleContribution() => new()
    {
        ContributionId = "CONT-2024-001",
        EmployerId = "EMP-001",
        EmployerName = "Acme Corporation Pty Ltd",
        EmployerABN = "51824753556",
        PayPeriodEndDate = new DateTime(2024, 6, 30, 0, 0, 0, DateTimeKind.Utc),
        TotalContribution = 1625.00m,
        Currency = "AUD",
        PaymentReference = "PAY-REF-20240630",
        Members = new ContributionMembers
        {
            Member = new List<MemberContribution>
            {
                new() { MemberAccountNumber = "SF-100001", MemberName = "Jane Smith",   ContributionType = "SuperannuationGuarantee", GrossAmount = 875.00m },
                new() { MemberAccountNumber = "SF-100002", MemberName = "John Citizen", ContributionType = "SuperannuationGuarantee", GrossAmount = 750.00m }
            }
        }
    };

    [Fact]
    public void Transform_SetsCorrectAllocationIdPrefix()
    {
        var contribution = BuildSampleContribution();

        var result = _sut.Transform(contribution);

        Assert.StartsWith("FA-", result.AllocationId);
        Assert.Equal("FA-CONT-2024-001", result.AllocationId);
    }

    [Fact]
    public void Transform_MapsEmployerDetailsCorrectly()
    {
        var contribution = BuildSampleContribution();

        var result = _sut.Transform(contribution);

        Assert.Equal(contribution.EmployerId, result.EmployerDetails.EmployerId);
        Assert.Equal(contribution.EmployerName, result.EmployerDetails.EmployerName);
        Assert.Equal(contribution.EmployerABN, result.EmployerDetails.ABN);
    }

    [Fact]
    public void Transform_MapsMemberAllocationsCorrectly()
    {
        var contribution = BuildSampleContribution();

        var result = _sut.Transform(contribution);

        Assert.Equal(2, result.MemberAllocations.Allocation.Count);

        var first = result.MemberAllocations.Allocation[0];
        Assert.Equal("SF-100001", first.AccountNumber);
        Assert.Equal("Jane Smith", first.MemberName);
        Assert.Equal("SuperannuationGuarantee", first.ContributionType);
        Assert.Equal(875.00m, first.ContributionAmount);
        Assert.Equal("PENDING", first.AllocationStatus);

        var second = result.MemberAllocations.Allocation[1];
        Assert.Equal("SF-100002", second.AccountNumber);
        Assert.Equal(750.00m, second.ContributionAmount);
    }

    [Fact]
    public void Transform_SetsStatusToPending()
    {
        var contribution = BuildSampleContribution();

        var result = _sut.Transform(contribution);

        Assert.Equal("PENDING", result.Status);
    }

    [Fact]
    public void Transform_SetsAllocationStatusToPendingForEachMember()
    {
        var contribution = BuildSampleContribution();

        var result = _sut.Transform(contribution);

        Assert.All(result.MemberAllocations.Allocation, a => Assert.Equal("PENDING", a.AllocationStatus));
    }

    [Fact]
    public void Transform_PreservesTotalAllocated()
    {
        var contribution = BuildSampleContribution();

        var result = _sut.Transform(contribution);

        Assert.Equal(1625.00m, result.TotalAllocated);
        Assert.Equal("AUD", result.CurrencyCode);
    }

    [Fact]
    public void Transform_SetsSourceContributionRef()
    {
        var contribution = BuildSampleContribution();

        var result = _sut.Transform(contribution);

        Assert.Equal(contribution.ContributionId, result.SourceContributionRef);
    }

    [Fact]
    public void Transform_SetsAllocationDate()
    {
        var contribution = BuildSampleContribution();

        var result = _sut.Transform(contribution);

        Assert.Equal(contribution.PayPeriodEndDate, result.AllocationDate);
    }

    [Fact]
    public void Transform_ThrowsArgumentNullException_WhenContributionIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.Transform(null!));
    }
}
