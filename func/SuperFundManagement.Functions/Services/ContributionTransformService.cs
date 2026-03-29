using System.Text.RegularExpressions;
using SuperFundManagement.Functions.Models;

namespace SuperFundManagement.Functions.Services;

/// <summary>
/// Migrated equivalent of the BizTalk ContributionToAllocationMap + ContributionMapHelper.
/// Transforms a SuperContributionRequest into a FundAllocationInstruction applying the
/// same business rules previously implemented via BizTalk Mapper Scripting Functoids.
/// </summary>
public class ContributionTransformService
{
    /// <summary>
    /// Transforms the incoming super contribution request into a fund allocation instruction.
    /// Replicates the four BizTalk functoids:
    ///   Functoid 1 – String Concatenate  : AllocationId = "FA-" + ContributionId
    ///   Functoid 2 – String Constant     : AllocationStatus = "PENDING" (per member)
    ///   Functoid 3 – Scripting (FormatABN): formats ABN as "XX XXX XXX XXX"
    ///   Functoid 4 – Scripting (CalculateNetContribution): applies 15% contributions tax
    /// </summary>
    public FundAllocationInstruction Transform(SuperContributionRequest request)
    {
        var allocations = request.Members.Member.Select(m => new Allocation
        {
            AccountNumber = m.MemberAccountNumber,
            MemberName = m.MemberName,
            ContributionType = m.ContributionType,
            ContributionAmount = CalculateNetContribution(m.GrossAmount),
            AllocationStatus = "PENDING"
        }).ToList();

        return new FundAllocationInstruction
        {
            AllocationId = $"FA-{request.ContributionId}",
            SourceContributionRef = request.ContributionId,
            EmployerDetails = new EmployerDetails
            {
                EmployerId = request.EmployerId,
                EmployerName = request.EmployerName,
                ABN = FormatABN(request.EmployerABN)
            },
            AllocationDate = request.PayPeriodEndDate,
            MemberAllocations = new MemberAllocationsContainer { Allocation = allocations },
            TotalAllocated = request.TotalContribution,
            CurrencyCode = request.Currency,
            Status = "PENDING"
        };
    }

    /// <summary>
    /// Strips whitespace from an Australian Business Number (ABN) and formats it as
    /// the standard "XX XXX XXX XXX" display format.
    /// Migrated from BizTalk Scripting Functoid 3 / ContributionMapHelper.FormatABN.
    /// </summary>
    public static string FormatABN(string abn)
    {
        if (string.IsNullOrWhiteSpace(abn))
            return abn ?? string.Empty;

        string digits = Regex.Replace(abn, @"[\s\-]", string.Empty);

        if (digits.Length != 11 || !Regex.IsMatch(digits, @"^\d{11}$"))
            return abn;

        return $"{digits[..2]} {digits[2..5]} {digits[5..8]} {digits[8..11]}";
    }

    /// <summary>
    /// Calculates the net superannuation contribution after applying the standard
    /// 15% contributions tax rate: NetAmount = GrossAmount × 0.85.
    /// Migrated from BizTalk Scripting Functoid 4 / ContributionMapHelper.CalculateNetContribution.
    /// </summary>
    public static decimal CalculateNetContribution(decimal grossAmount)
    {
        const decimal contributionsTaxRate = 0.15m;
        return Math.Round(grossAmount * (1m - contributionsTaxRate), 2, MidpointRounding.AwayFromZero);
    }
}
