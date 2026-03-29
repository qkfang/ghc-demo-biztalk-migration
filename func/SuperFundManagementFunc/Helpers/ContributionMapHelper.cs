using System.Text.RegularExpressions;

namespace SuperFundManagementFunc.Helpers;

/// <summary>
/// Azure Functions equivalent of the BizTalk Scripting Functoid helper class
/// (<c>SuperFundManagement.Maps.ContributionMapHelper</c> in the BizTalk project).
///
/// In BizTalk, these methods are embedded as inline C# inside &lt;msxsl:script&gt; blocks
/// in the generated XSLT (<c>ContributionToAllocationMap.xsl</c>), or called from the
/// external assembly via the Scripting Functoid's "External Assembly" mode.
///
/// In the Azure Functions migration, they are called directly from
/// <see cref="SuperFundManagementFunc.Services.ContributionTransformService"/>.
///
/// Functoid 3 → <see cref="FormatABN"/>
/// Functoid 4 → <see cref="CalculateNetContribution"/>
/// </summary>
public static class ContributionMapHelper
{
    // ── Scripting Functoid 3 — FormatABN ─────────────────────────────────────
    // BizTalk equivalent wiring:
    //   Input  : /SuperContributionRequest/EmployerABN
    //   Output : /FundAllocationInstruction/EmployerDetails/ABN

    /// <summary>
    /// Strips whitespace from an Australian Business Number (ABN) and formats
    /// it as the standard "XX XXX XXX XXX" display format.
    /// If the cleaned value is not exactly 11 digits the original string is
    /// returned unchanged so downstream processing is never blocked by bad data.
    /// </summary>
    /// <param name="abn">Raw ABN value as it arrives in the XML message.</param>
    /// <returns>Formatted ABN string, e.g. "51 824 753 556".</returns>
    public static string FormatABN(string abn)
    {
        if (string.IsNullOrWhiteSpace(abn))
            return abn ?? string.Empty;

        // Remove any existing whitespace or hyphens
        string digits = Regex.Replace(abn, @"[\s\-]", string.Empty);

        if (digits.Length != 11 || !Regex.IsMatch(digits, @"^\d{11}$"))
            return abn; // Not a valid 11-digit ABN – return as-is

        // Format: XX XXX XXX XXX
        return $"{digits[..2]} {digits[2..5]} {digits[5..8]} {digits[8..11]}";
    }

    // ── Scripting Functoid 4 — CalculateNetContribution ──────────────────────
    // BizTalk equivalent wiring:
    //   Input  : /SuperContributionRequest/Members/Member/GrossAmount
    //   Output : /FundAllocationInstruction/MemberAllocations/Allocation/ContributionAmount

    /// <summary>
    /// Calculates the net superannuation contribution after applying the standard
    /// 15% contributions tax rate mandated by the ATO for concessional contributions.
    ///   NetAmount = GrossAmount × (1 − 0.15) = GrossAmount × 0.85
    /// The result is rounded to two decimal places.
    /// </summary>
    /// <param name="grossAmount">Gross contribution amount.</param>
    /// <returns>Net contribution amount rounded to two decimal places.</returns>
    public static decimal CalculateNetContribution(decimal grossAmount)
    {
        const decimal contributionsTaxRate = 0.15m;
        return Math.Round(grossAmount * (1m - contributionsTaxRate), 2, MidpointRounding.AwayFromZero);
    }
}
