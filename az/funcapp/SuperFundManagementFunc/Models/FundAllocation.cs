using System.Collections.Generic;
using System.Xml.Serialization;

namespace SuperFundManagement.Functions;

[XmlRoot("FundAllocationInstruction", Namespace = "http://SuperFundManagement.Schemas.FundAllocation")]
public class FundAllocationInstruction
{
    public string AllocationId { get; set; } = string.Empty;
    public string SourceContributionRef { get; set; } = string.Empty;
    public EmployerDetails EmployerDetails { get; set; } = new EmployerDetails();
    public string AllocationDate { get; set; } = string.Empty;
    public MemberAllocations MemberAllocations { get; set; } = new MemberAllocations();
    public decimal TotalAllocated { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING";
}

public class EmployerDetails
{
    public string EmployerId { get; set; } = string.Empty;
    public string EmployerName { get; set; } = string.Empty;
    public string ABN { get; set; } = string.Empty;
}

public class MemberAllocations
{
    [XmlElement("Allocation")]
    public List<Allocation> Allocation { get; set; } = new List<Allocation>();
}

public class Allocation
{
    public string AccountNumber { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public string ContributionType { get; set; } = string.Empty;
    public decimal ContributionAmount { get; set; }
    public string AllocationStatus { get; set; } = "PENDING";
}
