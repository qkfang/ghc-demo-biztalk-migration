using System.Collections.Generic;
using System.Xml.Serialization;

namespace SuperFundManagement.Functions;

[XmlRoot("SuperContributionRequest", Namespace = "http://SuperFundManagement.Schemas.SuperContribution")]
public class SuperContributionRequest
{
    public string ContributionId { get; set; } = string.Empty;
    public string EmployerId { get; set; } = string.Empty;
    public string EmployerName { get; set; } = string.Empty;
    public string EmployerABN { get; set; } = string.Empty;
    public string PayPeriodEndDate { get; set; } = string.Empty;
    public Members Members { get; set; } = new Members();
    public decimal TotalContribution { get; set; }
    public string Currency { get; set; } = "AUD";
    public string PaymentReference { get; set; } = string.Empty;
}

public class Members
{
    [XmlElement("Member")]
    public List<Member> Member { get; set; } = new List<Member>();
}

public class Member
{
    public string MemberAccountNumber { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public string ContributionType { get; set; } = string.Empty;
    public decimal GrossAmount { get; set; }
}
