using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SuperFundManagement.Functions.Models;

namespace SuperFundManagement.Functions.Services
{
    public class ContributionTransformService : IContributionTransformService
    {
        public FundAllocationInstruction Transform(SuperContributionRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var allocations = (request.Members?.Member ?? new List<Member>())
                .Select(m => new Allocation
                {
                    AccountNumber      = m.MemberAccountNumber,
                    MemberName         = m.MemberName,
                    ContributionType   = m.ContributionType,
                    ContributionAmount = CalculateNetContribution(m.GrossAmount),
                    AllocationStatus   = "PENDING"
                })
                .ToList();

            return new FundAllocationInstruction
            {
                AllocationId          = $"FA-{request.ContributionId}",
                SourceContributionRef = request.ContributionId,
                EmployerDetails = new EmployerDetails
                {
                    EmployerId   = request.EmployerId,
                    EmployerName = request.EmployerName,
                    ABN          = FormatABN(request.EmployerABN)
                },
                AllocationDate = request.PayPeriodEndDate,
                MemberAllocations = new MemberAllocationsWrapper
                {
                    Allocation = allocations
                },
                TotalAllocated = allocations.Sum(a => a.ContributionAmount),
                CurrencyCode   = request.Currency,
                Status         = "PENDING"
            };
        }

        private static string FormatABN(string abn)
        {
            if (string.IsNullOrWhiteSpace(abn))
                return abn ?? string.Empty;

            var digits = Regex.Replace(abn, @"[\s\-]", string.Empty);
            if (digits.Length != 11 || !Regex.IsMatch(digits, @"^\d{11}$"))
                return abn;

            return $"{digits[..2]} {digits[2..5]} {digits[5..8]} {digits[8..11]}";
        }

        private static decimal CalculateNetContribution(decimal gross)
            => Math.Round(gross * 0.85m, 2, MidpointRounding.AwayFromZero);
    }
}
