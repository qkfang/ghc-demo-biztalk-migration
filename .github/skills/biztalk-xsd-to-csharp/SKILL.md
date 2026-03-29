---
name: biztalk-xsd-to-csharp
description: 'Convert BizTalk XSD schema files to C# model classes with XML serialization attributes. Use when: migrating BizTalk XSD schemas, generating C# models from XSD, converting BizTalk message types, XSD to POCO, create C# classes from schema, BizTalk schema migration to .NET 8.'
argument-hint: 'Path to the .xsd file(s) and target C# namespace'
---

# BizTalk XSD → C# Model Classes

Convert BizTalk `.xsd` schema files into strongly-typed C# model classes decorated with `System.Xml.Serialization` attributes for use in Azure Functions (.NET 8 isolated worker).

## When to Use

- Migrating a BizTalk XSD schema to a C# class
- Need XML-serializable POCOs that match the original BizTalk message structure
- Setting up the `Models/` layer before writing transform or function code

## Procedure

### Step 1 — Read the XSD
Read the target `.xsd` file. Note:
- `targetNamespace` → becomes the `[XmlRoot(Namespace = "...")]` value
- `root_reference` annotation → the root class name
- `xs:element` / `xs:complexType` nesting → class / property hierarchy
- `xs:string` → `string`, `xs:decimal` → `decimal`, `xs:date` → `DateTime`, `xs:boolean` → `bool`
- `maxOccurs="unbounded"` → `List<T>` with `[XmlArray]` + `[XmlArrayItem]`

### Step 2 — Generate the C# Class

Apply these conventions:

| XSD construct | C# output |
|---|---|
| Root element | `public class <Name>` decorated with `[XmlRoot("<Name>", Namespace = "<ns>")]` |
| Simple child element | `public <Type> <Name> { get; set; }` with `[XmlElement("<Name>")]` |
| Complex child element | Nested `public class` with its own `[XmlElement]` property |
| `xs:sequence` of repeating elements | `public List<T> Items { get; set; }` with `[XmlArray("<Parent>")]` and `[XmlArrayItem("<Item>")]` |
| `default="..."` on XSD element | Initialise via `= "<value>";` in the property or constructor |
| `xs:date` | Use `public DateTime <Name> { get; set; }` |
| `xs:decimal` | Use `public decimal <Name> { get; set; }` |

### Step 3 — File Placement & Namespace

- Place generated files in `az/funcapp/<AppName>/Models/`
- Use namespace `<AppName>.Functions.Models`
- One `.cs` file per XSD root element (e.g. `SuperContribution.cs`, `FundAllocation.cs`)
- Add `using System.Collections.Generic;` and `using System.Xml.Serialization;` at the top

### Step 4 — Validate Completeness

Verify every XSD element maps to a C# property. Run a quick cross-check:
- Count `xs:element` declarations in XSD vs. properties in the class
- Confirm nested `xs:complexType` became nested classes (not flattened)
- Confirm `maxOccurs="unbounded"` elements use `List<T>` not arrays

## Example

**XSD input** (`SuperContributionSchema.xsd`, namespace `http://SuperFundManagement.Schemas.SuperContribution`):

```xml
<xs:element name="SuperContributionRequest">
  <xs:complexType>
    <xs:sequence>
      <xs:element name="ContributionId" type="xs:string" />
      <xs:element name="Members">
        <xs:complexType>
          <xs:sequence>
            <xs:element name="Member" maxOccurs="unbounded">
              ...
```

**C# output** (`Models/SuperContribution.cs`):

```csharp
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

        [XmlElement("Members")]
        public MembersWrapper Members { get; set; }
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

        [XmlElement("GrossAmount")]
        public decimal GrossAmount { get; set; }
    }
}
```

## Anti-Patterns to Avoid

- Do **not** use `XmlSerializer` with `DataContract` — pick one serialization model
- Do **not** use `System.Xml.Linq.XElement` / `XDocument` for the model classes themselves
- Do **not** flatten nested `xs:complexType` into the parent class
- Do **not** use `[JsonProperty]` — these models are XML-only in the transform path
