# GitHub Copilot Demo Script
## BizTalk to Azure Functions Migration (~10 minutes)

---

### Setup Checklist (Before Demo)

- [ ] VS Code open with this repository
- [ ] GitHub Copilot extension installed and signed in
- [ ] Copilot Chat panel open (Ctrl+Shift+I)
- [ ] `func/OrderProcessingFunc` open in Explorer sidebar
- [ ] `biztalk/OrderProcessing/Maps/OrderToFulfillmentMap.btm` open in an editor tab
- [ ] Terminal open in `func/` folder
- [ ] Font size bumped up for visibility

---

## Step 1: Generate Function Scaffold (2 min)

**Presenter Notes:** Start by showing the BizTalk orchestration file briefly, then switch to the empty function.

### What to Show First

Open `biztalk/OrderProcessing/Orchestrations/OrderProcessingOrchestration.odx` and briefly say:

> "This is the BizTalk orchestration — XML-based, Designer-driven, requires a full BizTalk Server. Let's see how Copilot helps us recreate this as a modern Azure Function."

### Copilot Chat Prompt

Open a **new file** `OrderProcessingFunction_new.cs` in the Functions folder, then open Copilot Chat and type:

```
I have a BizTalk Server orchestration that:
1. Activates on an HTTP POST with an XML body (SourceOrder schema)
2. Transforms the XML using a map (SourceOrder → FulfillmentOrder)
3. POSTs the transformed XML to a downstream HTTP endpoint

Generate an Azure Functions v4 .NET 8 isolated worker C# class with:
- HTTP trigger on POST /api/orders
- XML deserialization using XmlSerializer
- Dependency injection of IOrderTransformService and IFulfillmentSenderService
- Returns 202 Accepted with a JSON body { fulfillmentId, status }
- Returns 400 on validation errors
- Returns 502 if the downstream service fails
```

### Expected Copilot Output

Copilot generates a complete function class including:
- Constructor injection
- `XmlSerializer.Deserialize()` for the request body
- `ValidateOrder()` helper method
- Try/catch with appropriate HTTP status codes
- `ILogger` usage

### Talking Points

> "Notice how Copilot understood the BizTalk pattern — receive, transform, send — and translated it directly to Azure Functions concepts: HTTP trigger, service injection, and proper error codes. This would have taken a developer half a day to write from scratch."

---

## Step 2: Refactor Legacy C# Logic (3 min)

**Presenter Notes:** Show the BizTalk map file, then use Copilot to explain and refactor the transform logic.

### What to Show First

Open `biztalk/OrderProcessing/Maps/OrderToFulfillmentMap.btm` and point at the functoid sections:

> "Here's the BizTalk map — it uses drag-and-drop 'functoids'. The String Concatenate functoid adds 'FF-' to the OrderId. The Multiplication functoid computes LineTotal. Copilot can read this and write the equivalent C# code."

### Copilot Chat Prompt (in context of the .btm file open)

```
Looking at this BizTalk map file, explain what each functoid does 
and then rewrite the entire transformation as a clean C# method 
in OrderTransformService that maps SourceOrder to FulfillmentOrder.

The method should:
- Prefix FulfillmentId with "FF-"
- Map CustomerDetails fields
- Use LINQ to transform Items into LineItems
- Calculate LineTotal as Quantity * UnitPrice
- Set Status to "PENDING"
- Map ShippingAddress to ShipTo
```

### Expected Copilot Output

```csharp
public FulfillmentOrder Transform(SourceOrder order)
{
    return new FulfillmentOrder
    {
        FulfillmentId = $"FF-{order.OrderId}",
        SourceOrderRef = order.OrderId,
        CustomerDetails = new CustomerDetails
        {
            Id = order.CustomerId,
            Name = order.CustomerName,
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

Open `func/OrderProcessingFunc.Tests/Services/OrderTransformServiceTests.cs` and say:

> "We have the service written. Now let's ask Copilot to generate comprehensive unit tests — the kind that would have required a full test strategy document with BizTalk."

### Copilot Chat Prompt (with OrderTransformService.cs open in context)

```
Generate comprehensive xUnit tests for OrderTransformService. 
Include tests for:
1. FulfillmentId gets "FF-" prefix (Assert.StartsWith)
2. CustomerDetails fields are mapped correctly
3. LineItems are mapped with correct count and LineTotal calculation (Qty × Price)
4. Status is always "PENDING"
5. ShippingAddress is correctly mapped to ShipTo
6. OrderTotal is preserved
7. ArgumentNullException is thrown for null input

Use a private helper method BuildSampleOrder() that returns a realistic SourceOrder.
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

Open `func/OrderProcessingFunc/Services/OrderTransformService.cs` and highlight the class without XML docs.

### Copilot Chat Prompt

```
Add XML documentation comments to this OrderTransformService class and 
its Transform method. The comments should:
- Explain that this replaces the BizTalk OrderToFulfillmentMap.btm
- Document each significant mapping step in the method body
- Reference the BizTalk equivalent functoid names in inline comments
- Add <param> and <returns> tags
```

### Expected Copilot Output

```csharp
/// <summary>
/// Implements the order-to-fulfillment transformation logic.
/// This is the Azure Functions equivalent of the BizTalk OrderToFulfillmentMap.btm,
/// including the String Concatenate functoid (FF- prefix) and Multiplication functoid (LineTotal).
/// </summary>
public class OrderTransformService : IOrderTransformService
{
    /// <summary>
    /// Transforms a <see cref="SourceOrder"/> into a <see cref="FulfillmentOrder"/>,
    /// applying all business rules equivalent to the BizTalk map functoids.
    /// </summary>
    /// <param name="order">The incoming source order to transform.</param>
    /// <returns>A fully populated <see cref="FulfillmentOrder"/> ready to dispatch.</returns>
    public FulfillmentOrder Transform(SourceOrder order)
    {
        return new FulfillmentOrder
        {
            // BizTalk equivalent: String Concatenate functoid("FF-", OrderId)
            FulfillmentId = $"FF-{order.OrderId}",
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

Select all content in `OrderProcessingOrchestration.odx`, open Copilot Chat:

```
Explain what this BizTalk ODX orchestration file does in plain English, 
and identify what Azure services or patterns would replace each component.
```

### Bonus: Validate XML Schema

```
Given this XSD schema, generate a valid sample XML document I can use 
to test the OrderProcessingFunction HTTP endpoint with curl.
```
