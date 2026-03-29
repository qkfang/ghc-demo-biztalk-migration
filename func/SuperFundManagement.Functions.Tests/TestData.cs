using SuperFundManagement.Functions.Models;

namespace SuperFundManagement.Functions.Tests;

/// <summary>
/// Shared test data matching the BizTalk sample files in biztalk/SuperFundManagement/Samples/.
/// </summary>
internal static class TestData
{
    internal static SuperContributionRequest BuildSampleRequest() => new()
    {
        ContributionId = "CONT-2024-001",
        EmployerId = "EMP-001",
        EmployerName = "Acme Corporation Pty Ltd",
        EmployerABN = "51824753556",
        PayPeriodEndDate = "2024-06-30",
        Members = new MembersContainer
        {
            Member =
            [
                new Member
                {
                    MemberAccountNumber = "SF-100001",
                    MemberName = "Jane Smith",
                    ContributionType = "SuperannuationGuarantee",
                    GrossAmount = 875.00m
                },
                new Member
                {
                    MemberAccountNumber = "SF-100002",
                    MemberName = "John Citizen",
                    ContributionType = "SuperannuationGuarantee",
                    GrossAmount = 750.00m
                }
            ]
        },
        TotalContribution = 1625.00m,
        Currency = "AUD",
        PaymentReference = "PAY-REF-20240630"
    };
}
