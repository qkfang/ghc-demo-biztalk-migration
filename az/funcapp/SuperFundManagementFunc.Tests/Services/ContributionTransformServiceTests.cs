using System;
using System.Collections.Generic;
using FluentAssertions;
using SuperFundManagement.Functions.Models;
using SuperFundManagement.Functions.Services;
using Xunit;

namespace SuperFundManagement.Functions.Tests.Services
{
    public class ContributionTransformServiceTests
    {
        private readonly ContributionTransformService _sut = new();

        private static SuperContributionRequest BuildRequest(
            string contributionId = "C-001",
            string employerAbn = "51824753556",
            decimal memberGross = 1000m,
            int memberCount = 1) => new()
        {
            ContributionId    = contributionId,
            EmployerId        = "EMP-1",
            EmployerName      = "Acme Corp",
            EmployerABN       = employerAbn,
            PayPeriodEndDate  = new DateTime(2024, 6, 30),
            TotalContribution = memberGross * memberCount,
            Currency          = "AUD",
            PaymentReference  = "PAY-REF-001",
            Members = new MembersWrapper
            {
                Member = BuildMembers(memberGross, memberCount)
            }
        };

        private static List<Member> BuildMembers(decimal gross, int count)
        {
            var list = new List<Member>();
            for (int i = 0; i < count; i++)
                list.Add(new Member
                {
                    MemberAccountNumber = $"ACC-{i + 1:000}",
                    MemberName          = $"Member {i + 1}",
                    ContributionType    = "Concessional",
                    GrossAmount         = gross
                });
            return list;
        }

        [Fact]
        public void Transform_ValidRequest_ReturnsCorrectAllocationId()
        {
            var request = BuildRequest(contributionId: "C-042");
            var result = _sut.Transform(request);
            result.AllocationId.Should().Be("FA-C-042");
        }

        [Fact]
        public void Transform_ValidRequest_SourceContributionRefMatchesContributionId()
        {
            var request = BuildRequest(contributionId: "C-042");
            var result = _sut.Transform(request);
            result.SourceContributionRef.Should().Be("C-042");
        }

        [Fact]
        public void Transform_ValidRequest_FormatsAbnCorrectly()
        {
            var request = BuildRequest(employerAbn: "51824753556");
            var result = _sut.Transform(request);
            result.EmployerDetails.ABN.Should().Be("51 824 753 556");
        }

        [Fact]
        public void Transform_InvalidAbn_ReturnsRawAbn()
        {
            var request = BuildRequest(employerAbn: "INVALID");
            var result = _sut.Transform(request);
            result.EmployerDetails.ABN.Should().Be("INVALID");
        }

        [Fact]
        public void Transform_NullAbn_ReturnsEmptyString()
        {
            var request = BuildRequest(employerAbn: null!);
            var result = _sut.Transform(request);
            result.EmployerDetails.ABN.Should().Be(string.Empty);
        }

        [Fact]
        public void Transform_ValidRequest_CalculatesNetContributionCorrectly()
        {
            var request = BuildRequest(memberGross: 1000m);
            var result = _sut.Transform(request);
            result.MemberAllocations.Allocation[0].ContributionAmount.Should().Be(850.00m);
        }

        [Fact]
        public void Transform_ValidRequest_AllocationStatusIsPending()
        {
            var request = BuildRequest();
            var result = _sut.Transform(request);
            result.MemberAllocations.Allocation[0].AllocationStatus.Should().Be("PENDING");
        }

        [Fact]
        public void Transform_ValidRequest_StatusIsPending()
        {
            var request = BuildRequest();
            var result = _sut.Transform(request);
            result.Status.Should().Be("PENDING");
        }

        [Fact]
        public void Transform_ValidRequest_TotalAllocatedIsSumOfNetAmounts()
        {
            var request = BuildRequest(memberGross: 1000m, memberCount: 3);
            var result = _sut.Transform(request);
            result.TotalAllocated.Should().Be(2550.00m);
        }

        [Fact]
        public void Transform_EmptyMembers_ReturnsEmptyAllocationsAndZeroTotal()
        {
            var request = BuildRequest();
            request.Members.Member.Clear();

            var result = _sut.Transform(request);

            result.MemberAllocations.Allocation.Should().BeEmpty();
            result.TotalAllocated.Should().Be(0m);
        }

        [Fact]
        public void Transform_NullMembers_ReturnsEmptyAllocationsAndZeroTotal()
        {
            var request = BuildRequest();
            request.Members = null!;

            var result = _sut.Transform(request);

            result.MemberAllocations.Allocation.Should().BeEmpty();
            result.TotalAllocated.Should().Be(0m);
        }

        [Fact]
        public void Transform_NullRequest_ThrowsArgumentNullException()
        {
            var act = () => _sut.Transform(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Transform_ZeroGrossAmount_ContributionAmountIsZero()
        {
            var request = BuildRequest(memberGross: 0m);
            var result = _sut.Transform(request);
            result.MemberAllocations.Allocation[0].ContributionAmount.Should().Be(0m);
        }

        [Fact]
        public void Transform_ValidRequest_CopiesEmployerDetails()
        {
            var request = BuildRequest();
            var result = _sut.Transform(request);
            result.EmployerDetails.EmployerId.Should().Be("EMP-1");
            result.EmployerDetails.EmployerName.Should().Be("Acme Corp");
        }

        [Fact]
        public void Transform_ValidRequest_AllocationDateMatchesPayPeriodEndDate()
        {
            var request = BuildRequest();
            var result = _sut.Transform(request);
            result.AllocationDate.Should().Be(new DateTime(2024, 6, 30));
        }

        [Fact]
        public void Transform_ValidRequest_CurrencyCodeMatchesCurrency()
        {
            var request = BuildRequest();
            var result = _sut.Transform(request);
            result.CurrencyCode.Should().Be("AUD");
        }

        [Fact]
        public void Transform_HighPrecisionGross_RoundsCorrectly()
        {
            var request = BuildRequest(memberGross: 1234.56m);
            var result = _sut.Transform(request);
            // 1234.56 * 0.85 = 1049.376 → rounds to 1049.38
            result.MemberAllocations.Allocation[0].ContributionAmount.Should().Be(1049.38m);
        }
    }
}
