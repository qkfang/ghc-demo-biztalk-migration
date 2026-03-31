using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace SuperFundManagement.Functions;

public class ContributionTransformService : IContributionTransformService
{
    private readonly ILogger<ContributionTransformService> _logger;

    public ContributionTransformService(ILogger<ContributionTransformService> logger)
    {
        _logger = logger;
    }

    public FundAllocationInstruction Transform(SuperContributionRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _logger.LogInformation("Transforming contribution {ContributionId}", request.ContributionId);

        var instruction = new FundAllocationInstruction
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
            MemberAllocations = new MemberAllocations
            {
                Allocation = request.Members?.Member?.Select(m => new Allocation
                {
                    AccountNumber = m.MemberAccountNumber,
                    MemberName = m.MemberName,
                    ContributionType = m.ContributionType,
                    ContributionAmount = CalculateNetContribution(m.GrossAmount),
                    AllocationStatus = "PENDING"
                }).ToList() ?? new System.Collections.Generic.List<Allocation>()
            },
            TotalAllocated = request.TotalContribution,
            CurrencyCode = request.Currency,
            Status = "PENDING"
        };

        return instruction;
    }

    private static string FormatABN(string abn)
    {
        if (string.IsNullOrWhiteSpace(abn))
            return abn ?? string.Empty;
        string digits = Regex.Replace(abn, @"[\s\-]", string.Empty);
        if (digits.Length != 11 || !Regex.IsMatch(digits, @"^\d{11}$"))
            return abn;
        return $"{digits.Substring(0, 2)} {digits.Substring(2, 3)} {digits.Substring(5, 3)} {digits.Substring(8, 3)}";
    }

    private static decimal CalculateNetContribution(decimal gross)
    {
        const decimal contributionsTaxRate = 0.15m;
        return Math.Round(gross * (1m - contributionsTaxRate), 2, MidpointRounding.AwayFromZero);
    }
}
