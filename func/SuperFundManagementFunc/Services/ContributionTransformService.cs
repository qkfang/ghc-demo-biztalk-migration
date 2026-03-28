using SuperFundManagementFunc.Models;

namespace SuperFundManagementFunc.Services;

/// <summary>
/// Implements the superannuation contribution-to-allocation transformation logic.
/// This is the Azure Functions equivalent of the BizTalk ContributionToAllocationMap.btm,
/// including the String Concatenate functoid (FA- prefix) and the constant PENDING functoid.
/// </summary>
public class ContributionTransformService : IContributionTransformService
{
    private const string AllocationIdPrefix = "FA-";
    private const string DefaultStatus = "PENDING";

    /// <inheritdoc />
    public FundAllocation Transform(SuperContribution contribution)
    {
        ArgumentNullException.ThrowIfNull(contribution);

        return new FundAllocation
        {
            // BizTalk equivalent: String Concatenate functoid("FA-", ContributionId)
            AllocationId = AllocationIdPrefix + contribution.ContributionId,

            // Direct field mappings
            SourceContributionRef = contribution.ContributionId,
            AllocationDate = contribution.PayPeriodEndDate,
            TotalAllocated = contribution.TotalContribution,
            CurrencyCode = contribution.Currency,
            Status = DefaultStatus,

            EmployerDetails = new EmployerDetails
            {
                EmployerId = contribution.EmployerId,
                EmployerName = contribution.EmployerName,
                ABN = contribution.EmployerABN
            },

            // BizTalk equivalent: Looping functoid over Members/Member → MemberAllocations/Allocation
            MemberAllocations = new MemberAllocations
            {
                Allocation = contribution.Members.Member.Select(member => new MemberAllocation
                {
                    AccountNumber = member.MemberAccountNumber,
                    MemberName = member.MemberName,
                    ContributionType = member.ContributionType,
                    ContributionAmount = member.GrossAmount,
                    // BizTalk equivalent: String Constant functoid → "PENDING"
                    AllocationStatus = DefaultStatus
                }).ToList()
            }
        };
    }
}
