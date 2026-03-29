using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SuperFundManagement.Functions.Models
{
    [XmlRoot("SuperContributionRequest",
             Namespace = "http://SuperFundManagement.Schemas.SuperContribution")]
    public class SuperContributionRequest
    {
        [XmlElement("ContributionId")]
        public string ContributionId { get; set; }

        [XmlElement("EmployerId")]
        public string EmployerId { get; set; }

        [XmlElement("EmployerName")]
        public string EmployerName { get; set; }

        [XmlElement("EmployerABN")]
        public string EmployerABN { get; set; }

        [XmlElement("PayPeriodEndDate", DataType = "date")]
        public DateTime PayPeriodEndDate { get; set; }

        [XmlElement("Members")]
        public MembersWrapper Members { get; set; }

        [XmlElement("TotalContribution")]
        public decimal TotalContribution { get; set; }

        [XmlElement("Currency")]
        public string Currency { get; set; } = "AUD";

        [XmlElement("PaymentReference")]
        public string PaymentReference { get; set; }
    }

    public class MembersWrapper
    {
        [XmlElement("Member")]
        public List<Member> Member { get; set; } = new();
    }

    public class Member
    {
        [XmlElement("MemberAccountNumber")]
        public string MemberAccountNumber { get; set; }

        [XmlElement("MemberName")]
        public string MemberName { get; set; }

        [XmlElement("ContributionType")]
        public string ContributionType { get; set; }

        [XmlElement("GrossAmount")]
        public decimal GrossAmount { get; set; }
    }
}
