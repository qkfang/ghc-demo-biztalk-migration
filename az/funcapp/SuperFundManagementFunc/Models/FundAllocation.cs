using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SuperFundManagement.Functions.Models
{
    [XmlRoot("FundAllocationInstruction",
             Namespace = "http://SuperFundManagement.Schemas.FundAllocation")]
    public class FundAllocationInstruction
    {
        [XmlElement("AllocationId")]
        public string AllocationId { get; set; }

        [XmlElement("SourceContributionRef")]
        public string SourceContributionRef { get; set; }

        [XmlElement("EmployerDetails")]
        public EmployerDetails EmployerDetails { get; set; }

        [XmlElement("AllocationDate", DataType = "date")]
        public DateTime AllocationDate { get; set; }

        [XmlElement("MemberAllocations")]
        public MemberAllocationsWrapper MemberAllocations { get; set; }

        [XmlElement("TotalAllocated")]
        public decimal TotalAllocated { get; set; }

        [XmlElement("CurrencyCode")]
        public string CurrencyCode { get; set; }

        [XmlElement("Status")]
        public string Status { get; set; } = "PENDING";
    }

    public class EmployerDetails
    {
        [XmlElement("EmployerId")]
        public string EmployerId { get; set; }

        [XmlElement("EmployerName")]
        public string EmployerName { get; set; }

        [XmlElement("ABN")]
        public string ABN { get; set; }
    }

    public class MemberAllocationsWrapper
    {
        [XmlElement("Allocation")]
        public List<Allocation> Allocation { get; set; } = new();
    }

    public class Allocation
    {
        [XmlElement("AccountNumber")]
        public string AccountNumber { get; set; }

        [XmlElement("MemberName")]
        public string MemberName { get; set; }

        [XmlElement("ContributionType")]
        public string ContributionType { get; set; }

        [XmlElement("ContributionAmount")]
        public decimal ContributionAmount { get; set; }

        [XmlElement("AllocationStatus")]
        public string AllocationStatus { get; set; } = "PENDING";
    }
}
