using System.Xml.Serialization;

namespace SuperFundManagementFunc.Models;

/// <summary>
/// Represents an incoming superannuation contribution request from an employer's payroll system.
/// Mirrors the SuperContributionSchema.xsd BizTalk schema.
/// </summary>
[XmlRoot("SuperContributionRequest", Namespace = "http://SuperFundManagement.Schemas.SuperContribution")]
public class SuperContribution
{
    [XmlElement("ContributionId")]
    public string ContributionId { get; set; } = string.Empty;

    [XmlElement("EmployerId")]
    public string EmployerId { get; set; } = string.Empty;

    [XmlElement("EmployerName")]
    public string EmployerName { get; set; } = string.Empty;

    [XmlElement("EmployerABN")]
    public string EmployerABN { get; set; } = string.Empty;

    [XmlElement("PayPeriodEndDate", DataType = "date")]
    public DateTime PayPeriodEndDate { get; set; }

    [XmlElement("Members")]
    public ContributionMembers Members { get; set; } = new();

    [XmlElement("TotalContribution")]
    public decimal TotalContribution { get; set; }

    [XmlElement("Currency")]
    public string Currency { get; set; } = "AUD";

    [XmlElement("PaymentReference")]
    public string PaymentReference { get; set; } = string.Empty;
}

/// <summary>
/// Container for member contribution entries in the contribution request.
/// </summary>
public class ContributionMembers
{
    [XmlElement("Member")]
    public List<MemberContribution> Member { get; set; } = new();
}

/// <summary>
/// A single member's superannuation contribution details.
/// </summary>
public class MemberContribution
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
