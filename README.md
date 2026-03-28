# BizTalk to Azure Functions Migration Demo

A complete, live demo repository for the **"GitHub Copilot: BizTalk to Azure Functions Migration"** demonstration. It shows how GitHub Copilot accelerates the migration of a legacy BizTalk Server 2020 integration to a cloud-native Azure Functions v4 (.NET 8) solution.

---

## Demo Scenario

An **Order Processing** integration that:

1. **Receives** an HTTP POST with an XML `OrderRequest` payload
2. **Transforms** the payload from the legacy `OrderRequest` format to the modern `FulfillmentOrder` format (equivalent to a BizTalk map with String Concatenate and Multiplication functoids)
3. **Forwards** the transformed payload as XML to a downstream HTTP fulfillment endpoint

---

## Repository Structure

```
.
├── biztalk/                        # BizTalk Server 2020 source (the "before")
│   ├── OrderProcessing.sln
│   └── OrderProcessing/
│       ├── OrderProcessing.btproj
│       ├── BindingFile.xml         # Port & orchestration bindings
│       ├── Schemas/
│       │   ├── SourceOrderSchema.xsd
│       │   └── TargetFulfillmentSchema.xsd
│       ├── Maps/
│       │   └── OrderToFulfillmentMap.btm
│       ├── Orchestrations/
│       │   └── OrderProcessingOrchestration.odx
│       └── Pipelines/
│           ├── HttpReceivePipeline.btp
│           └── HttpSendPipeline.btp
│
├── func/                           # Azure Functions app (the "after")
│   ├── OrderProcessingFunc.sln
│   ├── OrderProcessingFunc/        # .NET 8 isolated worker function app
│   │   ├── Program.cs
│   │   ├── host.json
│   │   ├── local.settings.json
│   │   ├── Models/
│   │   │   ├── SourceOrder.cs
│   │   │   └── FulfillmentOrder.cs
│   │   ├── Services/
│   │   │   ├── IOrderTransformService.cs
│   │   │   ├── OrderTransformService.cs
│   │   │   ├── IFulfillmentSenderService.cs
│   │   │   └── FulfillmentSenderService.cs
│   │   └── Functions/
│   │       └── OrderProcessingFunction.cs
│   └── OrderProcessingFunc.Tests/  # xUnit test project
│       ├── Services/
│       │   ├── OrderTransformServiceTests.cs
│       │   └── FulfillmentSenderServiceTests.cs
│       └── Functions/
│           └── OrderProcessingFunctionTests.cs
│
├── bicep/                          # Azure IaC (Bicep)
│   ├── main.bicep
│   ├── main.bicepparam
│   └── modules/
│       ├── storage.bicep
│       ├── appServicePlan.bicep
│       ├── appInsights.bicep
│       └── functionApp.bicep
│
└── docs/                           # Documentation
    ├── biztalk-orchestration.md
    ├── migration-guide.md
    ├── copilot-demo-script.md
    └── architecture.md
```

---

## Quick Start

### BizTalk Solution (reference only)

> Requires BizTalk Server 2020 + Visual Studio 2022 with BizTalk extensions.

```bash
# Open in Visual Studio
start biztalk/OrderProcessing.sln

# Build
msbuild biztalk/OrderProcessing.sln /p:Configuration=Release

# Deploy bindings after MSI install
BTSTask ImportBindings /ApplicationName:OrderProcessing /Source:biztalk/OrderProcessing/BindingFile.xml
```

See [biztalk/README.md](biztalk/README.md) for full deployment instructions.

### Azure Functions App

> Requires .NET 8 SDK + Azure Functions Core Tools v4.

```bash
# Install dependencies
cd func/OrderProcessingFunc
dotnet restore

# Run locally (start Azurite first for local storage)
func start

# Test with curl
curl -X POST http://localhost:7071/api/orders \
  -H "Content-Type: application/xml" \
  -d '<?xml version="1.0"?><OrderRequest xmlns="http://OrderProcessing.Schemas.SourceOrder"><OrderId>ORD-001</OrderId><CustomerId>CUST-42</CustomerId><CustomerName>Jane Smith</CustomerName><CustomerEmail>jane@example.com</CustomerEmail><OrderDate>2024-06-15T10:30:00Z</OrderDate><Items><Item><ProductCode>SKU-A</ProductCode><ProductName>Widget</ProductName><Quantity>2</Quantity><UnitPrice>29.99</UnitPrice></Item></Items><TotalAmount>59.98</TotalAmount><Currency>USD</Currency><ShippingAddress><Street>123 Main St</Street><City>Springfield</City><State>IL</State><ZipCode>62701</ZipCode><Country>US</Country></ShippingAddress></OrderRequest>'
```

Expected response:
```json
{ "fulfillmentId": "FF-ORD-001", "sourceOrderRef": "ORD-001", "status": "PENDING" }
```

See [func/README.md](func/README.md) for full details.

### Run Tests

```bash
cd func
dotnet test OrderProcessingFunc.sln --verbosity normal
```

### Deploy Infrastructure

```bash
az group create --name rg-order-processing-dev --location australiaeast

az deployment group create \
  --resource-group rg-order-processing-dev \
  --template-file bicep/main.bicep \
  --parameters bicep/main.bicepparam
```

See [bicep/README.md](bicep/README.md) for full deployment steps.

---

## Documentation

| Document | Description |
|---|---|
| [docs/architecture.md](docs/architecture.md) | Current vs target architecture with ASCII diagrams |
| [docs/biztalk-orchestration.md](docs/biztalk-orchestration.md) | BizTalk solution deep-dive |
| [docs/migration-guide.md](docs/migration-guide.md) | Step-by-step migration guide |
| [docs/copilot-demo-script.md](docs/copilot-demo-script.md) | Live demo script with Copilot prompts |

---

## Prerequisites

| Tool | Version | Required For |
|---|---|---|
| [Visual Studio 2022](https://visualstudio.microsoft.com/) | 17.x | BizTalk project (optional) |
| [BizTalk Server 2020](https://www.microsoft.com/biztalk) | 2020 | BizTalk deployment (optional) |
| [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | 8.x | Azure Functions app |
| [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local) | v4 | Local function run |
| [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) | 2.50+ | Bicep deployment |
| [Bicep CLI](https://learn.microsoft.com/azure/azure-resource-manager/bicep/install) | 0.24+ | Infrastructure deployment |
| [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) | latest | Local storage emulator |
| [GitHub Copilot](https://github.com/features/copilot) | any | Demo |