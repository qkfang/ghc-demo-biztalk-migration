---
name: biztalk-orchestration-to-function
description: 'Convert a BizTalk orchestration (.odx) and its binding file to an Azure Functions v4 HTTP-triggered function (.NET 8 isolated worker). Use when: migrating BizTalk orchestration, converting ODX file, porting receive port to HttpTrigger, porting send port to HttpClient service, BizTalk orchestration to Azure Functions, orchestration migration C#, SuperContributionOrchestration migration, BizTalk to Azure Functions C#.'
argument-hint: 'Path to .odx file and BindingFile.xml'
---

# BizTalk Orchestration (.odx) → Azure Functions HttpTrigger

Convert a BizTalk orchestration (`.odx`) and its `BindingFile.xml` into an Azure Functions v4 class using the .NET 8 **isolated worker** model.

## When to Use

- The orchestration wires together a Receive Port (HTTP) → Map → Send Port (HTTP)
- The output should be a single `[Function]`-decorated class that replaces the full orchestration flow
- Prerequisites: C# models (from `biztalk-xsd-to-csharp`) and transform service (from `biztalk-map-to-service`) already exist

## BizTalk → Azure Functions Mapping

| BizTalk concept | Azure Functions equivalent |
|---|---|
| `.odx` orchestration class | `[Function("<Name>")]` method in `Functions/<Name>Function.cs` |
| Receive Port (HTTP,`IsTwoWay="false"`) | `[HttpTrigger(AuthorizationLevel.Function, "post")]` |
| Send Port (HTTP) | `IFundAllocationSenderService` using `HttpClient` |
| Correlation Set on `ContributionId` | No equivalent needed — HTTP is stateless |
| `ActivationReceive` shape | Entry point of the function method |
| `Transform` shape (calls the map) | `_transformService.Transform(request)` |
| `Send` shape (calls the send port) | `await _senderService.SendAsync(instruction)` |
| `ReceiveLocation Address` from BindingFile | Route in `[HttpTrigger]`, e.g. `Route = "SuperFundManagement/Receive"` |
| Send port endpoint URL from BindingFile | `IConfiguration["FundAdminApiUrl"]` app setting |

## Procedure

### Step 1 — Read the ODX and BindingFile

From the `.odx`, extract:
- Orchestration name → function class name (`<Name>Function`)
- Input message type → input model class
- Output message type → output model class
- Port direction(`Receive` / `Send`) and `PortOperation` name

From `BindingFile.xml`, extract:
- `ReceiveLocation Address` → `Route` value for `[HttpTrigger]`
- Send port `Address` → default value for the `FundAdminApiUrl` app setting

### Step 2 — Create the Sender Service

Before writing the function, create the HTTP sender service that replaces the Send Port:

**Interface** (`Services/IFundAllocationSenderService.cs`):
```csharp
public interface IFundAllocationSenderService
{
    Task<HttpResponseMessage> SendAsync(FundAllocationInstruction instruction);
}
```

**Implementation** (`Services/FundAllocationSenderService.cs`):
```csharp
public class FundAllocationSenderService : IFundAllocationSenderService
{
    private readonly HttpClient _http;
    private readonly ILogger<FundAllocationSenderService> _logger;

    public FundAllocationSenderService(HttpClient http,
        ILogger<FundAllocationSenderService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<HttpResponseMessage> SendAsync(FundAllocationInstruction instruction)
    {
        var xml = SerializeToXml(instruction);
        var content = new StringContent(xml, Encoding.UTF8, "application/xml");
        _logger.LogInformation("Sending allocation {AllocationId}", instruction.AllocationId);
        return await _http.PostAsync(string.Empty, content);
    }

    private static string SerializeToXml<T>(T obj)
    {
        var ns = new XmlSerializerNamespaces();
        ns.Add(string.Empty, string.Empty);
        var serializer = new XmlSerializer(typeof(T));
        using var sw = new StringWriter();
        serializer.Serialize(sw, obj, ns);
        return sw.ToString();
    }
}
```

Register in `Program.cs`:
```csharp
services.AddHttpClient<IFundAllocationSenderService, FundAllocationSenderService>(client =>
    client.BaseAddress = new Uri(
        context.Configuration["FundAdminApiUrl"]
        ?? throw new InvalidOperationException("FundAdminApiUrl is required")));
```

### Step 3 — Create the Function Class

```csharp
// Functions/<OrchestratonName>Function.cs
[Function("SuperContribution")]
public async Task<HttpResponseData> RunAsync(
    [HttpTrigger(AuthorizationLevel.Function, "post",
                 Route = "SuperFundManagement/Receive")] HttpRequestData req)
{
    _logger.LogInformation("Received SuperContribution request");

    // 1. Deserialize incoming XML (replaces ActivationReceive shape)
    SuperContributionRequest request;
    try
    {
        var body = await req.ReadAsStringAsync();
        request = DeserializeXml<SuperContributionRequest>(body);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to deserialize request body");
        var bad = req.CreateResponse(HttpStatusCode.BadRequest);
        await bad.WriteStringAsync("Invalid XML payload");
        return bad;
    }

    // 2. Transform (replaces Transform shape + Map)
    var instruction = _transformService.Transform(request);

    // 3. Send (replaces Send shape + Send Port)
    HttpResponseMessage upstream;
    try
    {
        upstream = await _senderService.SendAsync(instruction);
        upstream.EnsureSuccessStatusCode();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to send allocation {AllocationId}", instruction.AllocationId);
        var err = req.CreateResponse(HttpStatusCode.BadGateway);
        await err.WriteStringAsync("Downstream allocation service error");
        return err;
    }

    _logger.LogInformation("Allocation {AllocationId} accepted", instruction.AllocationId);
    var ok = req.CreateResponse(HttpStatusCode.Accepted);
    await ok.WriteAsJsonAsync(new
    {
        instruction.AllocationId,
        instruction.Status,
        MemberCount = instruction.MemberAllocations?.Allocation?.Count ?? 0,
        instruction.TotalAllocated
    });
    return ok;
}
```

### Step 4 — Wire Up DI in Program.cs

Full `Program.cs` structure:

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddSingleton<IContributionTransformService, ContributionTransformService>();
        services.AddHttpClient<IFundAllocationSenderService, FundAllocationSenderService>(client =>
            client.BaseAddress = new Uri(
                context.Configuration["FundAdminApiUrl"]
                ?? throw new InvalidOperationException("FundAdminApiUrl is required")));
    })
    .Build();

await host.RunAsync();
```

### Step 5 — Configure App Settings

`local.settings.json`:
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "FundAdminApiUrl": "http://localhost:5000/api/allocations"
  }
}
```

The `FundAdminApiUrl` value comes from the Send Port `Address` in `BindingFile.xml`.

### Step 6 — Write Unit Tests

Create `Functions/<Name>FunctionTests.cs` covering:
- Happy path: valid XML body → `202 Accepted` with allocation summary
- Invalid / malformed XML body → `400 Bad Request`
- Transform service throws → `400 Bad Request` (caught at deserialization boundary)
- Sender service throws → `502 Bad Gateway`
- Sender service returns non-success status → `502 Bad Gateway`

Use `Moq` to mock `IContributionTransformService` and `IFundAllocationSenderService`.

## File Layout Summary

```
Functions/
  SuperContributionFunction.cs        ← replaces SuperContributionOrchestration.odx
Services/
  IFundAllocationSenderService.cs     ← replaces Send Port
  FundAllocationSenderService.cs
  IContributionTransformService.cs    ← (from biztalk-map-to-service skill)
  ContributionTransformService.cs
Program.cs                            ← DI wiring
local.settings.json                   ← from BindingFile.xml endpoint values
```

## Anti-Patterns to Avoid

- Do **not** create a `BackgroundService` or `IHostedService` — use `[HttpTrigger]` directly
- Do **not** hardcode the `FundAdminApiUrl` inside the function — always via `IConfiguration`
- Do **not** use `AuthorizationLevel.Anonymous` in production — use `Function` or higher
- Do **not** use `HttpClient` directly inside the function — always inject `IFundAllocationSenderService`
- Do **not** try to replicate BizTalk Correlation Sets — HTTP functions are stateless by design
