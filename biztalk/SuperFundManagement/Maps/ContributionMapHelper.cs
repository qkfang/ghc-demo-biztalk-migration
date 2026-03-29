using System;
using System.Text.RegularExpressions;

namespace SuperFundManagement.Maps
{
    /// <summary>
    /// Helper class used by BizTalk Mapper Scripting Functoids in ContributionToAllocationMap.btm.
    /// Methods in this class are referenced via the "External Assembly" mode of the Scripting Functoid,
    /// or embedded as inline C# inside &lt;msxsl:script&gt; blocks in the generated XSLT.
    ///
    /// Scripting Functoid 3 → FormatABN
    /// Scripting Functoid 4 → CalculateNetContribution
    /// </summary>
    public class ContributionMapHelper
    {
        // ── Scripting Functoid 3 ──────────────────────────────────────────────────
        // BizTalk wiring:
        //   Input  : /SuperContributionRequest/EmployerABN
        //   Output : /FundAllocationInstruction/EmployerDetails/ABN
        // Replaces the previous direct link (L05) with a scripted transformation.
        // ─────────────────────────────────────────────────────────────────────────

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
            return $"{digits.Substring(0, 2)} {digits.Substring(2, 3)} {digits.Substring(5, 3)} {digits.Substring(8, 3)}";
        }

        // ── Scripting Functoid 4 ──────────────────────────────────────────────────
        // BizTalk wiring:
        //   Input  : /SuperContributionRequest/Members/Member/GrossAmount
        //   Output : /FundAllocationInstruction/MemberAllocations/Allocation/ContributionAmount
        // Replaces the previous direct link (L11) with a scripted calculation.
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Calculates the net superannuation contribution after applying the standard
        /// 15% contributions tax rate mandated by the ATO for concessional contributions.
        ///   NetAmount = GrossAmount × (1 − 0.15) = GrossAmount × 0.85
        /// The result is rounded to two decimal places and returned as a string so it
        /// can be directly written into the target XML element by the XSLT engine.
        /// </summary>
        /// <param name="grossAmountStr">
        /// Gross contribution amount as a string, as it appears in the source XML.
        /// </param>
        /// <returns>
        /// Net contribution amount formatted to two decimal places, e.g. "743.75".
        /// Returns "0.00" when the input cannot be parsed.
        /// </returns>
        public static string CalculateNetContribution(string grossAmountStr)
        {
            if (!decimal.TryParse(grossAmountStr, out decimal gross))
                return "0.00";

            const decimal contributionsTaxRate = 0.15m;
            decimal net = Math.Round(gross * (1m - contributionsTaxRate), 2, MidpointRounding.AwayFromZero);
            return net.ToString("0.00");
        }
    }
}
