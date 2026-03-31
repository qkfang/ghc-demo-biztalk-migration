using System.Xml.Serialization;
using FluentAssertions;
using SuperFundFunctionApp.Models;
using SuperFundFunctionApp.Services;
using Xunit;

namespace SuperFundFunctionApp.Tests.Services;

/// <summary>
/// Integration tests for the complete transformation from SuperContributionRequest to FundAllocationInstruction.
/// Validates that the transformation matches BizTalk's ContributionToAllocationMap behavior.
/// </summary>
public class TransformationIntegrationTests
{
    private readonly TransformationService _service;

    public TransformationIntegrationTests()
    {
        _service = new TransformationService();
    }

    [Fact]
    public void Transform_ValidRequest_ProducesCorrectAllocationInstruction()
    {
        // Arrange - Sample data from BizTalk SampleInput.xml
        var request = new SuperContributionRequest
        {
            ContributionId = "CONT-2024-001",
            EmployerId = "EMP-001",
            EmployerName = "Acme Corporation Pty Ltd",
            EmployerABN = "51824753556",
            PayPeriodEndDate = "2024-06-30",
            Members = new MembersContainer
            {
                Member = new List<Member>
                {
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
                }
            },
            TotalContribution = 1625.00m,
            Currency = "AUD",
            PaymentReference = "PAY-REF-20240630"
        };

        // Act
        var result = _service.Transform(request);

        // Assert - Validate each field matches expected output from BizTalk SampleOutput.xml
        result.Should().NotBeNull();
        result.AllocationId.Should().Be("FA-CONT-2024-001"); // String concatenate functoid
        result.SourceContributionRef.Should().Be("CONT-2024-001");

        // Employer details
        result.EmployerDetails.Should().NotBeNull();
        result.EmployerDetails!.EmployerId.Should().Be("EMP-001");
        result.EmployerDetails.EmployerName.Should().Be("Acme Corporation Pty Ltd");
        result.EmployerDetails.ABN.Should().Be("51 824 753 556"); // FormatABN functoid

        result.AllocationDate.Should().Be("2024-06-30");

        // Member allocations
        result.MemberAllocations.Should().NotBeNull();
        result.MemberAllocations!.Allocation.Should().HaveCount(2);

        // First member
        var member1 = result.MemberAllocations.Allocation[0];
        member1.AccountNumber.Should().Be("SF-100001");
        member1.MemberName.Should().Be("Jane Smith");
        member1.ContributionType.Should().Be("SuperannuationGuarantee");
        member1.ContributionAmount.Should().Be(743.75m); // 875.00 * 0.85
        member1.AllocationStatus.Should().Be("PENDING");

        // Second member
        var member2 = result.MemberAllocations.Allocation[1];
        member2.AccountNumber.Should().Be("SF-100002");
        member2.MemberName.Should().Be("John Citizen");
        member2.ContributionType.Should().Be("SuperannuationGuarantee");
        member2.ContributionAmount.Should().Be(637.50m); // 750.00 * 0.85
        member2.AllocationStatus.Should().Be("PENDING");

        // Summary fields
        result.TotalAllocated.Should().Be(1625.00m);
        result.CurrencyCode.Should().Be("AUD");
        result.Status.Should().Be("PENDING");
    }

    [Fact]
    public void Transform_RequestWithNoMembers_ProducesEmptyAllocationList()
    {
        // Arrange
        var request = new SuperContributionRequest
        {
            ContributionId = "CONT-2024-002",
            EmployerId = "EMP-002",
            EmployerName = "Test Corp",
            EmployerABN = "12345678901",
            PayPeriodEndDate = "2024-07-31",
            Members = new MembersContainer { Member = new List<Member>() },
            TotalContribution = 0m,
            Currency = "AUD",
            PaymentReference = "PAY-REF-20240731"
        };

        // Act
        var result = _service.Transform(request);

        // Assert
        result.MemberAllocations.Should().NotBeNull();
        result.MemberAllocations!.Allocation.Should().BeEmpty();
    }

    [Fact]
    public void Transform_RequestWithSingleMember_TransformsCorrectly()
    {
        // Arrange
        var request = new SuperContributionRequest
        {
            ContributionId = "CONT-2024-003",
            EmployerId = "EMP-003",
            EmployerName = "Single Member Corp",
            EmployerABN = "98765432109",
            PayPeriodEndDate = "2024-08-31",
            Members = new MembersContainer
            {
                Member = new List<Member>
                {
                    new Member
                    {
                        MemberAccountNumber = "SF-200001",
                        MemberName = "Test User",
                        ContributionType = "SuperannuationGuarantee",
                        GrossAmount = 1000.00m
                    }
                }
            },
            TotalContribution = 1000.00m,
            Currency = "AUD",
            PaymentReference = "PAY-REF-20240831"
        };

        // Act
        var result = _service.Transform(request);

        // Assert
        result.AllocationId.Should().Be("FA-CONT-2024-003");
        result.MemberAllocations!.Allocation.Should().HaveCount(1);
        result.MemberAllocations.Allocation[0].ContributionAmount.Should().Be(850.00m); // 1000 * 0.85
    }

    [Fact]
    public void Transform_SerializedToXml_HasCorrectNamespace()
    {
        // Arrange
        var request = new SuperContributionRequest
        {
            ContributionId = "CONT-2024-004",
            EmployerId = "EMP-004",
            EmployerName = "XML Test Corp",
            EmployerABN = "11111111111",
            PayPeriodEndDate = "2024-09-30",
            Members = new MembersContainer { Member = new List<Member>() },
            TotalContribution = 0m,
            Currency = "AUD",
            PaymentReference = "PAY-REF-20240930"
        };

        // Act
        var result = _service.Transform(request);
        var xml = SerializeToXml(result);

        // Assert
        xml.Should().Contain("http://SuperFundManagement.Schemas.FundAllocation");
        xml.Should().Contain("<FundAllocationInstruction");
        xml.Should().Contain("<AllocationId>FA-CONT-2024-004</AllocationId>");
    }

    [Fact]
    public void Transform_DeserializeFromXml_AndTransform_RoundTripsCorrectly()
    {
        // Arrange - XML from BizTalk sample
        string inputXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<SuperContributionRequest xmlns=""http://SuperFundManagement.Schemas.SuperContribution"">
  <ContributionId>CONT-2024-001</ContributionId>
  <EmployerId>EMP-001</EmployerId>
  <EmployerName>Acme Corporation Pty Ltd</EmployerName>
  <EmployerABN>51824753556</EmployerABN>
  <PayPeriodEndDate>2024-06-30</PayPeriodEndDate>
  <Members>
    <Member>
      <MemberAccountNumber>SF-100001</MemberAccountNumber>
      <MemberName>Jane Smith</MemberName>
      <ContributionType>SuperannuationGuarantee</ContributionType>
      <GrossAmount>875.00</GrossAmount>
    </Member>
  </Members>
  <TotalContribution>875.00</TotalContribution>
  <Currency>AUD</Currency>
  <PaymentReference>PAY-REF-20240630</PaymentReference>
</SuperContributionRequest>";

        // Act
        var request = DeserializeXml<SuperContributionRequest>(inputXml);
        var result = _service.Transform(request!);
        var outputXml = SerializeToXml(result);

        // Assert
        request.Should().NotBeNull();
        result.Should().NotBeNull();
        outputXml.Should().Contain("FA-CONT-2024-001");
        outputXml.Should().Contain("51 824 753 556");
        outputXml.Should().Contain("743.75"); // 875 * 0.85
    }

    private static string SerializeToXml<T>(T obj) where T : class
    {
        var serializer = new XmlSerializer(typeof(T));
        using var writer = new StringWriter();
        serializer.Serialize(writer, obj);
        return writer.ToString();
    }

    private static T? DeserializeXml<T>(string xml) where T : class
    {
        var serializer = new XmlSerializer(typeof(T));
        using var reader = new StringReader(xml);
        return serializer.Deserialize(reader) as T;
    }
}
