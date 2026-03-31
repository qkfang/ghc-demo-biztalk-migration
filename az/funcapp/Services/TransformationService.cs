using System.Text.RegularExpressions;
using SuperFundFunctionApp.Models;

namespace SuperFundFunctionApp.Services;

/// <summary>
/// Transformation service that implements the business logic from BizTalk's ContributionToAllocationMap.
/// This replaces the XSLT-based transformation with C# code.
///
/// BizTalk Map: ContributionToAllocationMap.btm
/// Helper Class: ContributionMapHelper.cs
/// </summary>
public class TransformationService : ITransformationService
{
    /// <summary>
    /// Transforms a SuperContributionRequest into a FundAllocationInstruction.
    /// Implements all the mapping logic from ContributionToAllocationMap including:
    /// - String concatenation for AllocationId ("FA-" prefix)
    /// - ABN formatting (Functoid 3)
    /// - Net contribution calculation (Functoid 4)
    /// - Direct field mappings
    /// - Default constant values
    /// </summary>
    public FundAllocationInstruction Transform(SuperContributionRequest request)
    {
        var instruction = new FundAllocationInstruction
        {
            // String Concatenate: "FA-" + ContributionId
            AllocationId = $"FA-{request.ContributionId}",

            // Direct mapping
            SourceContributionRef = request.ContributionId,

            // Map employer details
            EmployerDetails = new EmployerDetails
            {
                EmployerId = request.EmployerId,
                EmployerName = request.EmployerName,
                // Scripting Functoid 3: FormatABN
                ABN = FormatABN(request.EmployerABN)
            },

            // Direct mapping
            AllocationDate = request.PayPeriodEndDate,

            // Transform member allocations
            MemberAllocations = new MemberAllocationsContainer
            {
                Allocation = request.Members?.Member.Select(member => new Allocation
                {
                    AccountNumber = member.MemberAccountNumber,
                    MemberName = member.MemberName,
                    ContributionType = member.ContributionType,
                    // Scripting Functoid 4: CalculateNetContribution
                    ContributionAmount = CalculateNetContribution(member.GrossAmount),
                    // String Constant
                    AllocationStatus = "PENDING"
                }).ToList() ?? new List<Allocation>()
            },

            // Direct mappings
            TotalAllocated = request.TotalContribution,
            CurrencyCode = request.Currency,

            // String Constant
            Status = "PENDING"
        };

        return instruction;
    }

    /// <summary>
    /// Formats an Australian Business Number (ABN) to standard "XX XXX XXX XXX" display format.
    /// Maps to BizTalk Scripting Functoid 3 (FormatABN) in ContributionMapHelper.cs.
    ///
    /// Example: "51824753556" → "51 824 753 556"
    /// </summary>
    /// <param name="abn">Raw ABN value (may contain spaces or hyphens)</param>
    /// <returns>Formatted ABN string, or original value if not valid</returns>
    public string FormatABN(string abn)
    {
        if (string.IsNullOrWhiteSpace(abn))
            return abn ?? string.Empty;

        // Remove any existing whitespace or hyphens
        string digits = Regex.Replace(abn, @"[\s\-]", string.Empty);

        if (digits.Length != 11 || !Regex.IsMatch(digits, @"^\d{11}$"))
            return abn; // Not a valid 11-digit ABN – return as-is

        // Format: XX XXX XXX XXX
        return $"{digits.Substring(0, 2)} {digits.Substring(2, 3)} {digits.Substring(5, 3)} {digits.Substring(8, 3)}";
    }

    /// <summary>
    /// Calculates the net superannuation contribution after applying the standard 15% contributions tax.
    /// Maps to BizTalk Scripting Functoid 4 (CalculateNetContribution) in ContributionMapHelper.cs.
    ///
    /// Formula: NetAmount = GrossAmount × 0.85
    ///
    /// Example: 875.00 → 743.75, 750.00 → 637.50
    /// </summary>
    /// <param name="grossAmount">Gross contribution amount</param>
    /// <returns>Net contribution amount rounded to 2 decimal places</returns>
    public decimal CalculateNetContribution(decimal grossAmount)
    {
        const decimal contributionsTaxRate = 0.15m;
        return Math.Round(grossAmount * (1m - contributionsTaxRate), 2, MidpointRounding.AwayFromZero);
    }
}
