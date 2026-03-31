using SuperFundFunctionApp.Models;

namespace SuperFundFunctionApp.Services;

/// <summary>
/// Service interface for transforming SuperContributionRequest to FundAllocationInstruction.
/// Implements the business logic from BizTalk's ContributionToAllocationMap.
/// </summary>
public interface ITransformationService
{
    /// <summary>
    /// Transforms a SuperContributionRequest into a FundAllocationInstruction.
    /// </summary>
    FundAllocationInstruction Transform(SuperContributionRequest request);

    /// <summary>
    /// Formats an Australian Business Number (ABN) to standard display format.
    /// Maps to BizTalk Scripting Functoid 3 (FormatABN).
    /// </summary>
    string FormatABN(string abn);

    /// <summary>
    /// Calculates net contribution after applying 15% contributions tax.
    /// Maps to BizTalk Scripting Functoid 4 (CalculateNetContribution).
    /// </summary>
    decimal CalculateNetContribution(decimal grossAmount);
}
