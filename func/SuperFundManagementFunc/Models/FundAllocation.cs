using System.Xml.Serialization;

namespace SuperFundManagementFunc.Models;

/// <summary>
/// Represents the outgoing fund allocation instruction sent to the fund administration platform.
/// Mirrors the FundAllocationSchema.xsd BizTalk schema.
/// </summary>
[XmlRoot("FundAllocationInstruction", Namespace = "http://SuperFundManagement.Schemas.FundAllocation")]
public class FundAllocation
{
    [XmlElement("AllocationId")]
    public string AllocationId { get; set; } = string.Empty;

    [XmlElement("SourceContributionRef")]
    public string SourceContributionRef { get; set; } = string.Empty;

    [XmlElement("EmployerDetails")]
    public EmployerDetails EmployerDetails { get; set; } = new();

    [XmlElement("AllocationDate", DataType = "date")]
    public DateTime AllocationDate { get; set; }

    [XmlElement("MemberAllocations")]
    public MemberAllocations MemberAllocations { get; set; } = new();

    [XmlElement("TotalAllocated")]
    public decimal TotalAllocated { get; set; }

    [XmlElement("CurrencyCode")]
    public string CurrencyCode { get; set; } = string.Empty;

    [XmlElement("Status")]
    public string Status { get; set; } = "PENDING";
}

/// <summary>
/// Employer details included in the fund allocation instruction.
/// </summary>
public class EmployerDetails
{
    [XmlElement("EmployerId")]
    public string EmployerId { get; set; } = string.Empty;

    [XmlElement("EmployerName")]
    public string EmployerName { get; set; } = string.Empty;

    [XmlElement("ABN")]
    public string ABN { get; set; } = string.Empty;
}

/// <summary>
/// Container for individual member allocation entries.
/// </summary>
public class MemberAllocations
{
    [XmlElement("Allocation")]
    public List<MemberAllocation> Allocation { get; set; } = new();
}

/// <summary>
/// A single member's allocation instruction within the fund allocation.
/// </summary>
public class MemberAllocation
{
    [XmlElement("AccountNumber")]
    public string AccountNumber { get; set; } = string.Empty;

    [XmlElement("MemberName")]
    public string MemberName { get; set; } = string.Empty;

    [XmlElement("ContributionType")]
    public string ContributionType { get; set; } = string.Empty;

    [XmlElement("ContributionAmount")]
    public decimal ContributionAmount { get; set; }

    [XmlElement("AllocationStatus")]
    public string AllocationStatus { get; set; } = "PENDING";
}
