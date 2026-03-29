using SuperFundManagementFunc.Helpers;
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
        // Scripting Functoid 3 (FormatABN): raw digits → "XX XXX XXX XXX"
        Assert.Equal("51 824 753 556", result.EmployerDetails.ABN);
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
        // Scripting Functoid 4 (CalculateNetContribution): 875.00 × 0.85 = 743.75
        Assert.Equal(743.75m, first.ContributionAmount);
        Assert.Equal("PENDING", first.AllocationStatus);

        var second = result.MemberAllocations.Allocation[1];
        Assert.Equal("SF-100002", second.AccountNumber);
        // Scripting Functoid 4 (CalculateNetContribution): 750.00 × 0.85 = 637.50
        Assert.Equal(637.50m, second.ContributionAmount);
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

/// <summary>
/// Unit tests for <see cref="ContributionMapHelper"/> — the Azure Functions equivalent
/// of the BizTalk Scripting Functoid helper methods.
/// </summary>
public class ContributionMapHelperTests
{
    // ── FormatABN (Scripting Functoid 3) ─────────────────────────────────────

    [Fact]
    public void FormatABN_FormatsElevenDigitsWithSpaces()
    {
        var result = ContributionMapHelper.FormatABN("51824753556");
        Assert.Equal("51 824 753 556", result);
    }

    [Fact]
    public void FormatABN_StripsExistingSpacesBeforeFormatting()
    {
        var result = ContributionMapHelper.FormatABN("51 824 753 556");
        Assert.Equal("51 824 753 556", result);
    }

    [Fact]
    public void FormatABN_StripsHyphensBeforeFormatting()
    {
        var result = ContributionMapHelper.FormatABN("51-824-753-556");
        Assert.Equal("51 824 753 556", result);
    }

    [Fact]
    public void FormatABN_ReturnsOriginal_WhenNotElevenDigits()
    {
        const string shortAbn = "1234567";
        var result = ContributionMapHelper.FormatABN(shortAbn);
        Assert.Equal(shortAbn, result);
    }

    [Fact]
    public void FormatABN_ReturnsEmpty_WhenInputIsEmpty()
    {
        Assert.Equal(string.Empty, ContributionMapHelper.FormatABN(string.Empty));
    }

    [Fact]
    public void FormatABN_ReturnsEmpty_WhenInputIsNull()
    {
        Assert.Equal(string.Empty, ContributionMapHelper.FormatABN(null!));
    }

    // ── CalculateNetContribution (Scripting Functoid 4) ──────────────────────

    [Fact]
    public void CalculateNetContribution_Applies15PercentTax()
    {
        // 875.00 × 0.85 = 743.75
        Assert.Equal(743.75m, ContributionMapHelper.CalculateNetContribution(875.00m));
    }

    [Fact]
    public void CalculateNetContribution_RoundsToTwoDecimalPlaces()
    {
        // 100.01 × 0.85 = 85.0085 → rounds to 85.01
        Assert.Equal(85.01m, ContributionMapHelper.CalculateNetContribution(100.01m));
    }

    [Fact]
    public void CalculateNetContribution_ReturnsZero_WhenGrossIsZero()
    {
        Assert.Equal(0.00m, ContributionMapHelper.CalculateNetContribution(0m));
    }
}
