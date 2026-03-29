---
name: biztalk-map-to-service
description: 'Convert BizTalk maps (.btm, .xsl) and Scripting Functoids (inline C# / external assembly) to a C# transform service for Azure Functions. Use when: migrating BizTalk map, converting BTM file, translating XSLT map, porting Scripting Functoid, String Concatenate functoid, String Constant functoid, inline C# functoid, external assembly functoid, BizTalk map migration, transform service C#, ContributionToAllocationMap migration.'
argument-hint: 'Path to .btm and/or .xsl file, plus the helper .cs file if present'
---

# BizTalk Map + Functoids → C# Transform Service

Convert a BizTalk `.btm` map (and its compiled `.xsl` output) into a strongly-typed C# service class that implements the same field-mapping and functoid logic in pure .NET 8 code.

## When to Use

- Migrating a BizTalk map to Azure Functions
- Translating XSLT / Scripting Functoids to C# helper methods
- Building the `Services/` layer before writing the function entry point

## Procedure

### Step 1 — Read the Map Artifacts

Read ALL of the following if present:
1. **`.xsl`** file — the compiled XSLT; look for `<xsl:value-of>`, `<xsl:for-each>`, and `<msxsl:script>` blocks
2. **`.btm`** file — the BizTalk Mapper metadata; shows link sources/targets and functoid chain
3. **Helper `.cs`** (e.g. `ContributionMapHelper.cs`) — contains the inline C# for any Scripting Functoids

Identify each mapping and classify it:

| XSLT / Functoid pattern | C# translation |
|---|---|
| Direct `<xsl:value-of select="src:Field" />` | Simple property assignment |
| String Concatenate functoid (`concat(...)`) | String interpolation `$"prefix{value}"` |
| String Constant functoid | Literal string assignment `= "CONSTANT"` |
| Scripting Functoid (inline C#) | Extract the `<![CDATA[...]]>` body as a `private static` method |
| Scripting Functoid (external assembly) | Copy the static method from the helper `.cs` file verbatim |
| `<xsl:for-each>` over a repeating node | `foreach` loop building a `List<T>` |

### Step 2 — Define the Service Interface

```csharp
// Services/IContributionTransformService.cs
namespace <AppName>.Functions.Services
{
    public interface IContributionTransformService
    {
        FundAllocationInstruction Transform(SuperContributionRequest request);
    }
}
```

### Step 3 — Implement the Transform Method

Structure the implementation as:

```
ContributionTransformService.cs
  Transform(request)
    ├─ Map root-level fields (ContributionId, EmployerDetails, etc.)
    ├─ Call private helper for each Scripting Functoid
    │    ├─ FormatABN(string abn) → "XX XXX XXX XXX"
    │    └─ CalculateNetContribution(decimal gross) → gross * 0.85m (15% contributions tax)
    ├─ foreach Member → build Allocation (for-each loop from XSLT)
    └─ Aggregate totals (TotalAllocated = sum of net amounts)
```

Key rules translated from the XSL functoids in this project:
- `AllocationId` = `"FA-"` + `ContributionId` (String Concatenate Functoid 1)
- `AllocationStatus` per member = `"PENDING"` (String Constant Functoid 2)
- `ABN` = `FormatABN(EmployerABN)` (Scripting Functoid 3)
- `ContributionAmount` = `CalculateNetContribution(GrossAmount)` (Scripting Functoid 4)

### Step 4 — Implement Private Helper Methods

Translate each `<msxsl:script>` or external-assembly method as a **`private static`** method in the same class:

```csharp
private static string FormatABN(string abn)
{
    if (string.IsNullOrWhiteSpace(abn)) return abn ?? string.Empty;
    var digits = Regex.Replace(abn, @"[\s\-]", string.Empty);
    if (digits.Length != 11 || !Regex.IsMatch(digits, @"^\d{11}$")) return abn;
    return $"{digits[..2]} {digits[2..5]} {digits[5..8]} {digits[8..11]}";
}

private static decimal CalculateNetContribution(decimal gross)
    => Math.Round(gross * 0.85m, 2);
```

### Step 5 — File Placement & Namespace

- Interface: `az/funcapp/<AppName>/Services/I<Name>TransformService.cs`
- Implementation: `az/funcapp/<AppName>/Services/<Name>TransformService.cs`
- Namespace: `<AppName>.Functions.Services`
- Add `using System.Text.RegularExpressions;` if `FormatABN` is on needed
- **Do not** add `ILogger` to the transform service (it is pure data transformation — no side effects)

### Step 6 — Register in DI

Add to `Program.cs`:

```csharp
services.AddSingleton<IContributionTransformService, ContributionTransformService>();
```

### Step 7 — Write Unit Tests

Corresponding test class: `<Name>TransformServiceTests.cs`

Cover:
- Happy path: valid request → correct AllocationId, ABN format, net amounts
- FormatABN with already-formatted / invalid / null input
- CalculateNetContribution: zero, positive, high precision
- Empty `Members` list → empty `MemberAllocations`, `TotalAllocated = 0`

## Full Implementation Template

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using <AppName>.Functions.Models;

namespace <AppName>.Functions.Services
{
    public class ContributionTransformService : IContributionTransformService
    {
        public FundAllocationInstruction Transform(SuperContributionRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var allocations = (request.Members?.Member ?? new List<Member>())
                .Select(m => new Allocation
                {
                    AccountNumber    = m.MemberAccountNumber,
                    MemberName       = m.MemberName,
                    ContributionType = m.ContributionType,
                    ContributionAmount = CalculateNetContribution(m.GrossAmount),
                    AllocationStatus = "PENDING"
                })
                .ToList();

            return new FundAllocationInstruction
            {
                AllocationId         = $"FA-{request.ContributionId}",
                SourceContributionRef = request.ContributionId,
                EmployerDetails = new EmployerDetails
                {
                    EmployerId   = request.EmployerId,
                    EmployerName = request.EmployerName,
                    ABN          = FormatABN(request.EmployerABN)
                },
                AllocationDate = request.PayPeriodEndDate,
                MemberAllocations = new MemberAllocations { Allocation = allocations },
                TotalAllocated = allocations.Sum(a => a.ContributionAmount),
                CurrencyCode   = request.Currency,
                Status         = "PENDING"
            };
        }

        private static string FormatABN(string abn)
        {
            if (string.IsNullOrWhiteSpace(abn)) return abn ?? string.Empty;
            var digits = Regex.Replace(abn, @"[\s\-]", string.Empty);
            if (digits.Length != 11 || !Regex.IsMatch(digits, @"^\d{11}$")) return abn;
            return $"{digits[..2]} {digits[2..5]} {digits[5..8]} {digits[8..11]}";
        }

        private static decimal CalculateNetContribution(decimal gross)
            => Math.Round(gross * 0.85m, 2);
    }
}
```

## Anti-Patterns to Avoid

- Do **not** use `XslCompiledTransform` in the C# service (translate the map to native C#, not XSLT)
- Do **not** put business logic in helper methods that require `ILogger` (keep them `private static`)
- Do **not** change the 15% tax rate or `"FA-"` prefix — these come from the original BizTalk map
- Do **not** call `Transform` with `null` — guard at the caller (function entry point level)
