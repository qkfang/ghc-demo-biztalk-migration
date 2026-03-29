using SuperFundManagement.Functions.Services;
using Xunit;

namespace SuperFundManagement.Functions.Tests;

/// <summary>
/// Unit tests for ContributionTransformService.
/// Validates the BizTalk Scripting Functoid logic migrated to C#.
/// </summary>
public class ContributionTransformServiceTests
{
    // ── FormatABN tests (Scripting Functoid 3) ────────────────────────────────

    [Fact]
    public void FormatABN_ValidElevenDigits_ReturnsFormattedABN()
    {
        var result = ContributionTransformService.FormatABN("51824753556");
        Assert.Equal("51 824 753 556", result);
    }

    [Fact]
    public void FormatABN_AlreadyFormattedWithSpaces_ReturnsFormattedABN()
    {
        var result = ContributionTransformService.FormatABN("51 824 753 556");
        Assert.Equal("51 824 753 556", result);
    }

    [Fact]
    public void FormatABN_WithHyphens_ReturnsFormattedABN()
    {
        var result = ContributionTransformService.FormatABN("51-824-753-556");
        Assert.Equal("51 824 753 556", result);
    }

    [Fact]
    public void FormatABN_InvalidLength_ReturnsOriginal()
    {
        var result = ContributionTransformService.FormatABN("1234");
        Assert.Equal("1234", result);
    }

    [Fact]
    public void FormatABN_NullInput_ReturnsEmpty()
    {
        var result = ContributionTransformService.FormatABN(null!);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void FormatABN_EmptyString_ReturnsEmpty()
    {
        var result = ContributionTransformService.FormatABN(string.Empty);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void FormatABN_NonNumericChars_ReturnsOriginal()
    {
        var result = ContributionTransformService.FormatABN("ABCDEFGHIJK");
        Assert.Equal("ABCDEFGHIJK", result);
    }

    // ── CalculateNetContribution tests (Scripting Functoid 4) ────────────────

    [Fact]
    public void CalculateNetContribution_875_Returns743Point75()
    {
        var result = ContributionTransformService.CalculateNetContribution(875.00m);
        Assert.Equal(743.75m, result);
    }

    [Fact]
    public void CalculateNetContribution_750_Returns637Point50()
    {
        var result = ContributionTransformService.CalculateNetContribution(750.00m);
        Assert.Equal(637.50m, result);
    }

    [Fact]
    public void CalculateNetContribution_Zero_ReturnsZero()
    {
        var result = ContributionTransformService.CalculateNetContribution(0m);
        Assert.Equal(0.00m, result);
    }

    [Fact]
    public void CalculateNetContribution_RoundingHalfUp_IsCorrect()
    {
        // 100.005 * 0.85 = 85.00425 → rounds to 85.00
        var result = ContributionTransformService.CalculateNetContribution(100.005m);
        Assert.Equal(85.00m, result);
    }

    // ── Transform (full map) tests ────────────────────────────────────────────

    [Fact]
    public void Transform_SampleInput_ProducesExpectedOutput()
    {
        var service = new ContributionTransformService();
        var request = TestData.BuildSampleRequest();

        var result = service.Transform(request);

        Assert.Equal("FA-CONT-2024-001", result.AllocationId);
        Assert.Equal("CONT-2024-001", result.SourceContributionRef);
        Assert.Equal("EMP-001", result.EmployerDetails.EmployerId);
        Assert.Equal("Acme Corporation Pty Ltd", result.EmployerDetails.EmployerName);
        Assert.Equal("51 824 753 556", result.EmployerDetails.ABN);
        Assert.Equal("2024-06-30", result.AllocationDate);
        Assert.Equal(1625.00m, result.TotalAllocated);
        Assert.Equal("AUD", result.CurrencyCode);
        Assert.Equal("PENDING", result.Status);
    }

    [Fact]
    public void Transform_SampleInput_AllocationsHaveCorrectNetAmounts()
    {
        var service = new ContributionTransformService();
        var request = TestData.BuildSampleRequest();

        var result = service.Transform(request);
        var allocations = result.MemberAllocations.Allocation;

        Assert.Equal(2, allocations.Count);

        Assert.Equal("SF-100001", allocations[0].AccountNumber);
        Assert.Equal("Jane Smith", allocations[0].MemberName);
        Assert.Equal("SuperannuationGuarantee", allocations[0].ContributionType);
        Assert.Equal(743.75m, allocations[0].ContributionAmount);
        Assert.Equal("PENDING", allocations[0].AllocationStatus);

        Assert.Equal("SF-100002", allocations[1].AccountNumber);
        Assert.Equal("John Citizen", allocations[1].MemberName);
        Assert.Equal(637.50m, allocations[1].ContributionAmount);
        Assert.Equal("PENDING", allocations[1].AllocationStatus);
    }

    [Fact]
    public void Transform_AllocationIdPrefixedWithFA()
    {
        var service = new ContributionTransformService();
        var request = TestData.BuildSampleRequest();
        request.ContributionId = "CONT-2099-999";

        var result = service.Transform(request);

        Assert.Equal("FA-CONT-2099-999", result.AllocationId);
    }
}
