# Copilot Migration Demo Guide


## Demo Guide

---

### Setup Checklist

- MCP tools enabled

---

## Step 1

Understand current BizTalk project

```
#agent create 'biztalk.md' mermaid diagram markdown file to describe current biztalk project.
biztalk source code is under `app-biztalk` folder. Keep it simple, include these:
- End-to-End Message Flow
- Schema Structures
- Field-by-Field Mapping Table
```

---

### Step 2

Create a customised BizTalk migration agent

```
/create-agent  create or update `biztalk-migration.agent.md` to include requirements and guildlines. Keep it simple, 
```

---

### Step 3

Use GitHub coding agent to create migration

```
#agent create an issue ticket in github copilot for biztalk migration

title: 'migrate Biztalk integration to Azure Functions'
body: 'migrate existing BizTalk application inside `biztalk` folder to new integration app on Azure.
- create the integraiton logics as a c# function app inside `az\funcapp`
- create tests for the integration inside `az\funcapp`
- create IaC deployment for azure inside `az\bicep`

keep the init migration process simple and as it as'

```

---

## Step 1: Generate Function Scaffold (2 min)

**Presenter Notes:** Start by showing the BizTalk orchestration file briefly, then switch to the empty function.

### What to Show First

Open `biztalk/SuperFundManagement/Orchestrations/SuperFundManagementOrchestration.odx` and briefly say:

> "This is the BizTalk orchestration — XML-based, Designer-driven, requires a full BizTalk Server. Let's see how Copilot helps us recreate this as a modern Azure Function."

### Copilot Chat Prompt

Open a **new file** `SuperFundManagementFunction_new.cs` in the Functions folder, then open Copilot Chat and type:

```
I have a BizTalk Server orchestration that:
1. Activates on an HTTP POST with an XML body (SuperContribution schema)
2. Transforms the XML using a map (SuperContribution → FundAllocationInstruction)
3. POSTs the transformed XML to a downstream HTTP endpoint

Generate an Azure Functions v4 .NET 8 isolated worker C# class with:
- HTTP trigger on POST /api/contributions
- XML deserialization using XmlSerializer
- Dependency injection of IContributionTransformService and IFundAllocationSenderService
- Returns 202 Accepted with a JSON body { allocationId, status }
- Returns 400 on validation errors
- Returns 502 if the downstream service fails
```


---

## Step 2: Refactor Legacy C# Logic (3 min)

**Presenter Notes:** Show the BizTalk map file, then use Copilot to explain and refactor the transform logic.

### What to Show First

Open `biztalk/SuperFundManagement/Maps/ContributionToAllocationMap.btm` and point at the functoid sections:

> "Here's the BizTalk map — it uses drag-and-drop 'functoids'. The String Concatenate functoid adds 'FA-' to the ContributionId. The Looping functoid iterates member allocations. Copilot can read this and write the equivalent C# code."

### Copilot Chat Prompt (in context of the .btm file open)

```
Looking at this BizTalk map file, explain what each functoid does 
and then rewrite the entire transformation as a clean C# method 
in ContributionTransformService that maps SuperContribution to FundAllocationInstruction.

The method should:
- Prefix AllocationId with "FA-"
- Map CustomerDetails fields
- Use LINQ to transform Items into LineItems
- Calculate LineTotal as Quantity * UnitPrice
- Set Status to "PENDING"
- Map ShippingAddress to ShipTo
```

### Expected Copilot Output

```csharp
public FundAllocationInstruction Transform(SuperContribution order)
{
    return new FundAllocationInstruction
    {
        AllocationId = $"FA-{contribution.ContributionId}",
        SourceContributionRef = contribution.ContributionId,
        CustomerDetails = new CustomerDetails
        {
            Id = contribution.EmployerId,
            Name = contribution.EmployerName,
            Email = order.CustomerEmail
        },
        LineItems = new FulfillmentLineItems
        {
            LineItem = order.Items.Item.Select(item => new LineItem
            {
                SKU = item.ProductCode,
                Description = item.ProductName,
                Qty = item.Quantity,
                Price = item.UnitPrice,
                LineTotal = item.Quantity * item.UnitPrice   // Multiplication functoid
            }).ToList()
        },
        // ... etc
    };
}
```

### Inline Chat Prompt (select the functoid comment in the .btm file)

With the multiplication functoid section selected, press **Ctrl+I** (inline chat) and type:

```
Convert this BizTalk multiplication functoid to C# equivalent code
```

### Talking Points

> "Copilot understands BizTalk XML artifacts — it reads the functoid type, the input parameters, and generates the exact C# equivalent. The 700-line XML map file becomes 50 lines of readable, testable C#."

---

## Step 3: Auto-Generate Unit Tests (3 min)

**Presenter Notes:** Show existing empty test file, then use Copilot to generate all tests.

### What to Show First

Open `az/funcapp/SuperFundManagementFunc.Tests/Services/ContributionTransformServiceTests.cs` and say:

> "We have the service written. Now let's ask Copilot to generate comprehensive unit tests — the kind that would have required a full test strategy document with BizTalk."

### Copilot Chat Prompt (with ContributionTransformService.cs open in context)

```
Generate comprehensive xUnit tests for ContributionTransformService. 
Include tests for:
1. AllocationId gets "FA-" prefix (Assert.StartsWith)
2. CustomerDetails fields are mapped correctly
3. LineItems are mapped with correct count and LineTotal calculation (Qty × Price)
4. Status is always "PENDING"
5. ShippingAddress is correctly mapped to ShipTo
6. OrderTotal is preserved
7. ArgumentNullException is thrown for null input

Use a private helper method BuildSampleOrder() that returns a realistic SuperContribution.
Show exact expected values in each assertion.
```

### Expected Copilot Output

Copilot generates all 7 test methods with:
- Descriptive names following the pattern `MethodName_Condition_ExpectedResult`
- Concrete assertion values (`Assert.Equal(89.97m, first.LineTotal)`)
- The `BuildSampleOrder()` helper shared across tests

### Inline Quick Test (press Ctrl+I on the Transform method)

```
/tests Generate a test that verifies LineTotal = Quantity * UnitPrice for each line item
```

### Run the tests

```bash
cd func && dotnet test --verbosity minimal
```

All tests pass immediately.

### Talking Points

> "With BizTalk, testing the map required deploying to a BizTalk server or using a limited BizTalk Unit Test Framework. Here, Copilot generated tests in seconds that run in milliseconds, with zero infrastructure. Notice the assertion on line `Assert.Equal(89.97m, first.LineTotal)` — Copilot calculated 3 × 29.99 for us."

---

## Step 4: Inline Documentation (2 min)

**Presenter Notes:** Show undocumented code, then use Copilot to add XML doc comments.

### What to Show First

Open `az/funcapp/SuperFundManagementaz/funcapp/Services/ContributionTransformService.cs` and highlight the class without XML docs.

### Copilot Chat Prompt

```
Add XML documentation comments to this ContributionTransformService class and 
its Transform method. The comments should:
- Explain that this replaces the BizTalk ContributionToAllocationMap.btm
- Document each significant mapping step in the method body
- Reference the BizTalk equivalent functoid names in inline comments
- Add <param> and <returns> tags
```

### Expected Copilot Output

```csharp
/// <summary>
/// Implements the order-to-fulfillment transformation logic.
/// This is the Azure Functions equivalent of the BizTalk ContributionToAllocationMap.btm,
/// including the String Concatenate functoid (FA- prefix) and Multiplication functoid (LineTotal).
/// </summary>
public class ContributionTransformService : IContributionTransformService
{
    /// <summary>
    /// Transforms a <see cref="SuperContribution"/> into a <see cref="FundAllocationInstruction"/>,
    /// applying all business rules equivalent to the BizTalk map functoids.
    /// </summary>
    /// <param name="order">The incoming source order to transform.</param>
    /// <returns>A fully populated <see cref="FundAllocationInstruction"/> ready to dispatch.</returns>
    public FundAllocationInstruction Transform(SuperContribution order)
    {
        return new FundAllocationInstruction
        {
            // BizTalk equivalent: String Concatenate functoid("FA-", OrderId)
            AllocationId = $"FA-{contribution.ContributionId}",
            // ...
        };
    }
}
```

### Inline Chat on a specific line

Select the `LineTotal = item.Quantity * item.UnitPrice` line, press **Ctrl+I**:

```
Add an inline comment referencing the BizTalk Multiplication functoid
```

Copilot adds:
```csharp
// BizTalk equivalent: Multiplication functoid(Quantity, UnitPrice)
LineTotal = item.Quantity * item.UnitPrice
```

### Talking Points

> "One of the underappreciated values of migration is documentation — BizTalk maps have almost no embedded documentation. Copilot can generate rich XML doc comments that also preserve the BizTalk lineage, which is invaluable for teams maintaining both systems during a transition period."

---

## Wrap-Up Talking Points (30 sec)

> "In 10 minutes, Copilot helped us:
> 1. **Scaffold** the Azure Function structure from a description of BizTalk behavior
> 2. **Refactor** a 700-line XML map file into 50 lines of readable C#
> 3. **Generate** a full test suite that runs in under 2 seconds
> 4. **Document** the code in a way that preserves institutional knowledge
>
> The same migration without Copilot would take an experienced BizTalk developer 2–3 days. With Copilot, it's measured in hours."

---

## Backup / Bonus Demos

### Bonus: Generate Bicep IaC

```
Generate Azure Bicep to deploy this .NET 8 isolated Azure Function with:
- Consumption plan (Y1/Dynamic)
- Storage account (Standard_LRS)
- Application Insights with Log Analytics workspace
- App setting for FulfillmentServiceUrl
- All resources tagged with environment and project
```

### Bonus: Explain BizTalk file

Select all content in `SuperFundManagementOrchestration.odx`, open Copilot Chat:

```
Explain what this BizTalk ODX orchestration file does in plain English, 
and identify what Azure services or patterns would replace each component.
```

### Bonus: Validate XML Schema

```
Given this XSD schema, generate a valid sample XML document I can use 
to test the SuperFundManagementFunction HTTP endpoint with curl.
```
