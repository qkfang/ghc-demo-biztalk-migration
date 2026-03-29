# Migration Guide: BizTalk Server → Azure Functions

## Why Migrate?

### Cost

| Factor              | BizTalk Server 2020                          | Azure Functions (Consumption)                 |
|---------------------|----------------------------------------------|-----------------------------------------------|
| Licensing           | ~$15,000–$50,000/server/year                 | Pay-per-execution (~$0.20 per 1M invocations) |
| Infrastructure      | Dedicated VMs + SQL Server cluster           | Serverless — no VM management                 |
| Operations          | BizTalk Admin + IIS + SQL DBA                | Managed platform, Azure Monitor               |
| Idle cost           | Always-on VMs billed 24/7                    | Zero cost when idle (Consumption plan)        |

### Scalability

BizTalk scales **vertically** (bigger VMs) or **horizontally** (additional BizTalk servers in a group). Both require manual configuration and significant overhead.

Azure Functions scales **automatically** and **instantly** — from 0 to hundreds of concurrent instances within seconds, driven by incoming HTTP load.

### Cloud-Native Benefits

- **DevOps integration**: CI/CD pipelines deploy a zip file — no BizTalk deployment MSIs
- **Observability**: Application Insights provides distributed tracing out of the box
- **Security**: Managed identities, Key Vault integration, no stored credentials
- **Portability**: Standard .NET 8 code — runs locally, in containers, or on any cloud

---

## Architecture Comparison

### BizTalk Server 2020 (Current State)

```
HTTP Client
    │
    │ POST /SuperFundManagement/Receive (port 7070)
    ▼
┌──────────────────────────────────────────────────────────────────────┐
│  BizTalk Server 2020                                                 │
│                                                                      │
│  IIS + ISAPI Extension                                               │
│      │                                                               │
│      ▼                                                               │
│  Receive Location (HTTP Adapter)                                     │
│      │ HttpReceivePipeline (XML Disassembler + Validator)            │
│      ▼                                                               │
│  MessageBox (SQL Server)                                             │
│      │                                                               │
│      ▼                                                               │
│  Orchestration (SuperFundManagementOrchestration.odx)                   │
│      ├─ Receive shape                                                │
│      ├─ Construct/Transform shape (ContributionToAllocationMap.btm)        │
│      └─ Send shape                                                   │
│      │                                                               │
│      ▼                                                               │
│  MessageBox (SQL Server)                                             │
│      │                                                               │
│      ▼                                                               │
│  Send Port (AllocationHttpSend)                                     │
│      │ HttpSendPipeline (XML Assembler)                              │
│      │ HTTP Adapter                                                   │
└──────────────────────────────────────────────────────────────────────┘
    │
    │ POST http://downstream-service/api/fulfillment
    ▼
Downstream Fulfillment Service
```

### Azure Functions v4 / .NET 8 (Target State)

```
HTTP Client
    │
    │ POST /api/contributions
    ▼
┌──────────────────────────────────────────────────────────────────────┐
│  Azure Functions (Consumption Plan)                                  │
│                                                                      │
│  HTTP Trigger: SuperContributionFunction.ProcessContribution()                │
│      │                                                               │
│      ├─ Deserialize XML → SuperContribution (XmlSerializer)                │
│      ├─ Validate (ContributionId, EmployerId, Members)                        │
│      ├─ IContributionTransformService.Transform()                           │
│      │       (equivalent of ContributionToAllocationMap.btm)              │
│      ├─ IFundAllocationSenderService.SendAsync()                        │
│      │       (equivalent of AllocationHttpSend port)               │
│      └─ Return 202 Accepted { allocationId, status }                │
└──────────────────────────────────────────────────────────────────────┘
    │
    │ POST https://downstream-service/api/fulfillment
    ▼
Downstream Fulfillment Service
```

---

## Component Mapping

| BizTalk Concept                    | Azure Functions Equivalent                                      |
|------------------------------------|-----------------------------------------------------------------|
| HTTP Receive Location              | `[HttpTrigger]` attribute on function                           |
| Receive Pipeline (XML Disassembler)| `XmlSerializer.Deserialize()` in function body                  |
| MessageBox                         | Function parameter / in-memory object                           |
| Orchestration                      | Azure Function class method                                     |
| Message Construction shape         | C# object instantiation (`new FundAllocationInstruction { ... }`)        |
| Transform shape + `.btm` map file  | `IContributionTransformService.Transform()` method                     |
| String Concatenate functoid        | C# string interpolation: `$"FA-{contribution.ContributionId}"`               |
| Multiplication functoid            | C# arithmetic: `item.Quantity * item.UnitPrice`                 |
| Looping functoid                   | LINQ `.Select()` on `Items.Item`                                |
| Send Pipeline (XML Assembler)      | `XmlSerializer.Serialize()` in `FundAllocationSenderService`       |
| HTTP Send Port                     | `IFundAllocationSenderService.SendAsync()` via `HttpClient`        |
| Binding File                       | `local.settings.json` / Azure App Settings                      |
| BTSTask deploy                     | `az functionapp deployment source config-zip`                   |
| BizTalk Admin Console              | Azure Portal / Azure CLI                                        |
| BizTalk Group Hub (monitoring)     | Application Insights + Log Analytics                            |

---

## Migration Steps

### Step 1: Understand the Existing BizTalk Solution

1. Export and review the current binding file (`BindingFile.xml`)
2. Open `SuperFundManagementOrchestration.odx` in Visual Studio to map the orchestration flow
3. Test the existing BizTalk solution with sample XML to capture expected inputs/outputs
4. Document the XSD schemas (`SuperContributionSchema.xsd`, `FundAllocationSchema.xsd`)
5. Export the XSLT generated from the BizTalk map (right-click `.btm` → Validate/Test Map)

### Step 2: Create the Azure Functions Project

1. Create a new .NET 8 isolated Azure Functions project:
   ```bash
   func init SuperFundManagementFunc --worker-runtime dotnet-isolated --target-framework net8.0
   ```
2. Add required NuGet packages:
   ```bash
   dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Http
   dotnet add package Microsoft.Extensions.Http
   ```

### Step 3: Port the Data Models

1. Create `SuperContribution.cs` — mirror of `SuperContributionSchema.xsd` with `[XmlRoot]`/`[XmlElement]` attributes
2. Create `FundAllocationInstruction.cs` — mirror of `FundAllocationSchema.xsd`
3. Validate by serializing/deserializing the sample XML documents captured in Step 1

### Step 4: Implement the Transformation Service

1. Create `IContributionTransformService` interface
2. Implement `ContributionTransformService`:
   - Replace String Concatenate functoid: `"FA-" + contribution.ContributionId`
   - Replace Multiplication functoid: `item.Quantity * item.UnitPrice`
   - Replace Looping functoid: `.Select(item => new LineItem { ... })`
3. Write unit tests for each transformation rule

### Step 5: Implement the Fulfillment Sender Service

1. Create `IFundAllocationSenderService` interface
2. Implement `FundAllocationSenderService` using `IHttpClientFactory`
3. Register `AddHttpClient()` in `Program.cs`
4. Read target URL from configuration (`FulfillmentServiceUrl`)

### Step 6: Implement the HTTP Trigger Function

1. Create `SuperFundManagementFunction` with `[HttpTrigger("post", Route = "orders")]`
2. Implement: deserialize XML → validate → transform → send → return 202
3. Add proper error handling: 400 for validation, 502 for downstream failure

### Step 7: Configure Local Settings

1. Update `local.settings.json`:
   ```json
   { "Values": { "FulfillmentServiceUrl": "http://localhost:7072/api/mock-fulfillment" } }
   ```
2. Start a mock HTTP server on port 7072 to simulate the downstream service

### Step 8: Test

1. Run all unit tests: `dotnet test`
2. Run integration test with real XML payload via `curl`
3. Compare response with the BizTalk output from Step 1

### Step 9: Deploy Infrastructure

1. Review and customize `bicep/main.bicepparam`
2. Deploy with `az deployment group create ...`
3. Verify all resources in Azure Portal

### Step 10: Deploy Function Code

```bash
cd az/funcapp/SuperFundManagementFunc
dotnet publish -c Release -o ./publish
cd publish && zip -r ../deploy.zip .
az functionapp deployment source config-zip \
  --resource-group rg-order-processing-dev \
  --name func-order-processingdev \
  --src ../deploy.zip
```

---

## Testing Strategy

| Level           | BizTalk Approach                               | Azure Functions Approach                        |
|-----------------|------------------------------------------------|-------------------------------------------------|
| Unit            | BizTalk Unit Test Framework (limited)          | xUnit + Moq (full isolation)                    |
| Schema/Map      | Test Map in BizTalk Mapper (manual)            | `XmlSerializer` round-trip tests               |
| Integration     | BizTalk Admin + HAT tracing                    | `func start` + `curl` + App Insights logs      |
| Load            | Visual Studio Load Test (deprecated)           | Azure Load Testing                              |
| End-to-End      | Deploy to BizTalk Dev server                   | Deploy to dev Function App slot                 |

---

## Deployment Differences

| Concern          | BizTalk                                          | Azure Functions                                |
|------------------|--------------------------------------------------|------------------------------------------------|
| Artifact         | MSI package + binding XML                        | Zip deployment package                         |
| Downtime         | Requires orchestration un-enlist/restart         | Zero-downtime slot swap                        |
| Rollback         | Re-import previous MSI + bindings                | Swap slots back or redeploy previous zip       |
| Configuration    | BizTalk Admin Console (GUI only)                 | `az functionapp config appsettings set`        |
| Monitoring       | BizTalk Admin Group Hub + SQL tracing            | Application Insights (queries, alerts, dashboards) |

---

## Rollback Plan

### Immediate rollback (Azure Functions)

1. In Azure Portal → Function App → Deployment Slots → swap `staging` ↔ `production`
2. Or redeploy the previous zip artifact:
   ```bash
   az functionapp deployment source config-zip \
     --resource-group rg-order-processing-prod \
     --name func-order-processingprod \
     --src ./previous-deploy.zip
   ```

### Fallback to BizTalk (during transition period)

While running both systems in parallel, maintain a feature flag or DNS-level routing:

1. Keep BizTalk application in `Enlisted` state (not `Started`)
2. If Azure Functions reports errors in Application Insights, update DNS/load balancer to route back to BizTalk endpoint
3. `BTSTask StartApplication /ApplicationName:SuperFundManagement` to re-activate BizTalk
4. Investigate and fix the Azure Functions issue, then re-cut over
