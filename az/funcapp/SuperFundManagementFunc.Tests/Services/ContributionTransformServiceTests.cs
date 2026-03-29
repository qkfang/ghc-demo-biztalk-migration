using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SuperFundManagement.Functions.Models;
using SuperFundManagement.Functions.Services;
using Xunit;

namespace SuperFundManagement.Functions.Tests.Services;

public class ContributionTransformServiceTests
{
    private readonly ContributionTransformService _sut = new(NullLogger<ContributionTransformService>.Instance);

    [Fact]
    public void Transform_HappyPath_ReturnsCorrectAllocation()
    {
        // Arrange
        var request = new SuperContributionRequest
        {
            ContributionId   = "CONT-2024-001",
            EmployerId       = "EMP-001",
            EmployerName     = "Acme Corp",
            EmployerABN      = "51824753556",
            PayPeriodEndDate = new DateTime(2024, 3, 31),
            TotalContribution = 875m,
            Currency         = "AUD",
            PaymentReference = "PAY-REF-001",
            Members = new MembersCollection
            {
                Member = new List<Member>
                {
                    new() { MemberAccountNumber = "ACC-001", MemberName = "Alice Smith", ContributionType = "SG", GrossAmount = 875m }
                }
            }
        };

        // Act
        var result = _sut.Transform(request);

        // Assert
        result.AllocationId.Should().Be("FA-CONT-2024-001");
        result.SourceContributionRef.Should().Be("CONT-2024-001");
        result.EmployerDetails.ABN.Should().Be("51 824 753 556");
        result.MemberAllocations.Allocation.Should().HaveCount(1);
        result.MemberAllocations.Allocation[0].ContributionAmount.Should().Be(743.75m);
        result.Status.Should().Be("PENDING");
        result.MemberAllocations.Allocation[0].AllocationStatus.Should().Be("PENDING");
    }

    [Fact]
    public void Transform_NullInput_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _sut.Transform(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("51824753556",   "51 824 753 556")]
    [InlineData("51 824 753 556","51 824 753 556")]
    [InlineData("51-824-753-556","51 824 753 556")]
    [InlineData("",              "")]
    [InlineData("12345",         "12345")]          // Not 11 digits — returned as-is
    public void Transform_FormatABN_VariousInputs(string rawAbn, string expected)
    {
        var request = new SuperContributionRequest
        {
            ContributionId   = "CONT-001",
            EmployerABN      = rawAbn,
            PayPeriodEndDate = DateTime.Today,
            Members          = new MembersCollection { Member = new List<Member>() }
        };

        var result = _sut.Transform(request);

        result.EmployerDetails.ABN.Should().Be(expected);
    }

    [Theory]
    [InlineData(875.0,   743.75)]
    [InlineData(1000.0,  850.00)]
    [InlineData(0.0,     0.00)]
    [InlineData(0.01,    0.01)]   // rounds up at 2dp
    public void Transform_CalculateNetContribution_CorrectRounding(double grossDouble, double expectedNetDouble)
    {
        decimal gross       = (decimal)grossDouble;
        decimal expectedNet = (decimal)expectedNetDouble;

        var request = new SuperContributionRequest
        {
            ContributionId   = "CONT-001",
            PayPeriodEndDate = DateTime.Today,
            Members          = new MembersCollection
            {
                Member = new List<Member>
                {
                    new() { MemberAccountNumber = "ACC", MemberName = "Test", ContributionType = "SG", GrossAmount = gross }
                }
            }
        };

        var result = _sut.Transform(request);

        result.MemberAllocations.Allocation[0].ContributionAmount.Should().Be(expectedNet);
    }
}
