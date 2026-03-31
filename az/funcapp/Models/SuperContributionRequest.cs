using System.Xml.Serialization;

namespace SuperFundFunctionApp.Models;

/// <summary>
/// Input message schema for superannuation contribution submissions.
/// Maps to BizTalk schema: SuperContributionSchema.xsd
/// Namespace: http://SuperFundManagement.Schemas.SuperContribution
/// </summary>
[XmlRoot("SuperContributionRequest", Namespace = "http://SuperFundManagement.Schemas.SuperContribution")]
public class SuperContributionRequest
{
    [XmlElement("ContributionId")]
    public string ContributionId { get; set; } = string.Empty;

    [XmlElement("EmployerId")]
    public string EmployerId { get; set; } = string.Empty;

    [XmlElement("EmployerName")]
    public string EmployerName { get; set; } = string.Empty;

    [XmlElement("EmployerABN")]
    public string EmployerABN { get; set; } = string.Empty;

    [XmlElement("PayPeriodEndDate")]
    public string PayPeriodEndDate { get; set; } = string.Empty;

    [XmlElement("Members")]
    public MembersContainer? Members { get; set; }

    [XmlElement("TotalContribution")]
    public decimal TotalContribution { get; set; }

    [XmlElement("Currency")]
    public string Currency { get; set; } = "AUD";

    [XmlElement("PaymentReference")]
    public string PaymentReference { get; set; } = string.Empty;
}

public class MembersContainer
{
    [XmlElement("Member")]
    public List<Member> Member { get; set; } = new();
}

public class Member
{
    [XmlElement("MemberAccountNumber")]
    public string MemberAccountNumber { get; set; } = string.Empty;

    [XmlElement("MemberName")]
    public string MemberName { get; set; } = string.Empty;

    [XmlElement("ContributionType")]
    public string ContributionType { get; set; } = string.Empty;

    [XmlElement("GrossAmount")]
    public decimal GrossAmount { get; set; }
}
