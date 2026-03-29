using SuperFundManagementFunc.Models;

namespace SuperFundManagementFunc.Services;

/// <summary>
/// Defines the contract for transforming a superannuation contribution request into a fund allocation instruction.
/// Replaces the BizTalk ContributionToAllocationMap.btm transformation.
/// </summary>
public interface IContributionTransformService
{
    /// <summary>
    /// Transforms a <see cref="SuperContribution"/> into a <see cref="FundAllocation"/>,
    /// applying all business rules equivalent to the BizTalk map functoids.
    /// </summary>
    /// <param name="contribution">The incoming superannuation contribution request to transform.</param>
    /// <returns>A fully populated <see cref="FundAllocation"/> ready to dispatch.</returns>
    FundAllocation Transform(SuperContribution contribution);
}
