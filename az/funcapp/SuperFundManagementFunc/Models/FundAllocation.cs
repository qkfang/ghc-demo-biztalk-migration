using System.Xml.Serialization;

namespace SuperFundManagement.Functions.Models;

[XmlRoot(ElementName = "FundAllocationInstruction", Namespace = "http://SuperFundManagement.Schemas.FundAllocation")]
public class FundAllocationInstruction
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
    public MemberAllocationsCollection MemberAllocations { get; set; } = new();

    [XmlElement("TotalAllocated")]
    public decimal TotalAllocated { get; set; }

    [XmlElement("CurrencyCode")]
    public string CurrencyCode { get; set; } = string.Empty;

    [XmlElement("Status")]
    public string Status { get; set; } = "PENDING";
}

public class EmployerDetails
{
    [XmlElement("EmployerId")]
    public string EmployerId { get; set; } = string.Empty;

    [XmlElement("EmployerName")]
    public string EmployerName { get; set; } = string.Empty;

    [XmlElement("ABN")]
    public string ABN { get; set; } = string.Empty;
}

public class MemberAllocationsCollection
{
    [XmlElement("Allocation")]
    public List<Allocation> Allocation { get; set; } = new();
}

public class Allocation
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
