# Architecture: BizTalk to Azure Functions

## Current State: BizTalk Server 2020

### Overview

The current superannuation fund management solution runs on **BizTalk Server 2020** — Microsoft's enterprise service bus and integration platform. It consists of tightly coupled components managed through the BizTalk Administration Console.

### Architecture Diagram

```mermaid
flowchart TB
    CLIENT(["🏢 API Consumer<br/>HTTP POST · application/xml"])

    subgraph ONPREM["On-Premises / IaaS Virtual Machine"]
        direction TB
        subgraph BTS["⚙️ BizTalk Server 2020 · IIS + ISAPI"]
            direction TB
            RL["📥 HTTP Receive Location<br/>BizTalk HTTP Adapter"]
            RP["Receive Pipeline<br/>XML Disassembly & Validation"]
            OE["Orchestration Engine<br/>XLANG/s Runtime"]
            MAP["🗺️ Message Map<br/>ContributionToAllocationMap.btm"]
            SP["📤 HTTP Send Port<br/>BizTalk HTTP Adapter"]
            RL --> RP --> OE --> MAP --> SP
        end
        subgraph SQL["🗄️ SQL Server"]
            direction LR
            MGMT[("BizTalk<br/>Management DB")]
            MSG[("MessageBox DB")]
        end
    end

    DS(["🏦 Downstream Fund<br/>Administration Service"])

    CLIENT -->|"HTTP POST · application/xml"| RL
    SP -->|"HTTP POST · application/xml"| DS
    RP -. "persist" .-> MSG
    MSG -. "route" .-> OE
    BTS -. "config" .-> MGMT

    classDef bts fill:#1B3A5C,stroke:#0D2440,color:#ffffff
    classDef db fill:#963A16,stroke:#6E2A0F,color:#ffffff
    classDef ext fill:#1B5E20,stroke:#0D3A11,color:#ffffff

    class RL,RP,OE,MAP,SP bts
    class MGMT,MSG db
    class CLIENT,DS ext
```

### Component Inventory

| Component                            | Technology                    | Purpose                                      |
|--------------------------------------|-------------------------------|----------------------------------------------|
| HTTP Receive Location                | BizTalk HTTP Adapter + IIS    | Inbound message ingestion                    |
| Receive Pipeline                     | `HttpReceivePipeline.btp`     | XML validation and disassembly               |
| MessageBox                           | SQL Server database           | Message persistence and routing              |
| Orchestration Engine                 | XLANG/s runtime               | Business process coordination                |
| Map                                  | `ContributionToAllocationMap.btm`   | XML transformation with functoids            |
| Send Pipeline                        | `HttpSendPipeline.btp`        | XML assembly and encoding                    |
| HTTP Send Port                       | BizTalk HTTP Adapter          | Outbound delivery                            |
| BizTalk Management DB                | SQL Server database           | Application metadata and bindings            |

---

## Target State: Azure Functions v4 / .NET 8

### Overview

The target solution runs as an **Azure Function** on the Consumption plan — fully serverless, with no persistent infrastructure to manage. The entire integration logic lives in a single, testable .NET 8 assembly.

### Architecture Diagram

```mermaid
flowchart TB
    CLIENT(["🏢 API Consumer<br/>HTTP POST · application/xml"])

    subgraph AZURE["☁️ Microsoft Azure"]
        direction TB
        subgraph FUNC["⚡ Azure Functions — Consumption Plan"]
            direction TB
            FN["📨 SuperContributionFunction<br/>HttpTrigger · POST /api/contributions"]
            DESER["1️⃣ Deserialize<br/>XmlSerializer.Deserialize()"]
            VALID["2️⃣ Validate<br/>Guard clauses on required fields"]
            TRANS["3️⃣ Transform<br/>IContributionTransformService.Transform()"]
            SEND["4️⃣ Dispatch<br/>IFundAllocationSenderService.SendAsync()"]
            RESP["5️⃣ Respond<br/>202 Accepted · { allocationId }"]
            FN --> DESER --> VALID --> TRANS --> SEND --> RESP
        end
        subgraph SUPPORT["Supporting Services"]
            direction LR
            AI["📊 Application Insights<br/>+ Log Analytics<br/>Traces · Metrics · Alerts"]
            STORE[("🗄️ Azure Storage<br/>AzureWebJobsStorage")]
        end
    end

    DS(["🏦 Downstream Fund<br/>Administration Service"])

    CLIENT -->|"HTTPS POST · application/xml"| FN
    SEND -->|"HTTPS POST · application/xml"| DS
    RESP -. "202 Accepted · allocationId" .-> CLIENT
    FUNC -.->|telemetry| AI
    FUNC -.->|"runtime state"| STORE

    classDef fn fill:#0058AB,stroke:#003D7A,color:#ffffff
    classDef support fill:#6B2FA0,stroke:#4A1B73,color:#ffffff
    classDef ext fill:#1B5E20,stroke:#0D3A11,color:#ffffff

    class FN,DESER,VALID,TRANS,SEND,RESP fn
    class AI,STORE support
    class CLIENT,DS ext
```

### Component Inventory

| Component                        | Technology                       | Purpose                                      |
|----------------------------------|----------------------------------|----------------------------------------------|
| HTTP Trigger                     | `[HttpTrigger]` attribute        | Inbound message ingestion                    |
| XML Deserialization              | `System.Xml.Serialization`       | Parse incoming `SuperContribution` XML             |
| Validation                       | C# guard clauses                 | Reject malformed or incomplete orders        |
| Transformation                   | `ContributionTransformService`          | Map `SuperContribution` → `FundAllocation`       |
| HTTP Dispatch                    | `FundAllocationSenderService`       | POST to downstream fund administration platform       |
| Configuration                    | Azure App Settings               | `FulfillmentServiceUrl` and other secrets    |
| Observability                    | Application Insights SDK         | Distributed tracing, metrics, alerting       |
| Storage                          | Azure Blob Storage               | Function runtime host management             |
| IaC                              | Bicep modules                    | Repeatable infrastructure deployment         |

---

## Technology Comparison

| Dimension                | BizTalk Server 2020                        | Azure Functions v4 (.NET 8)                |
|--------------------------|--------------------------------------------|--------------------------------------------|
| **Hosting**              | Windows Server VM (on-prem or IaaS)        | Serverless (Consumption plan)              |
| **Language**             | XLANG/s orchestration + C# components      | C# (.NET 8 isolated)                       |
| **Transformation**       | BizTalk Mapper (.btm) + XSLT               | LINQ + C# object mapping                  |
| **Configuration**        | Binding files + BizTalk Admin Console      | App Settings + `local.settings.json`       |
| **Deployment**           | MSI + BTSTask + Admin Console              | `az functionapp deployment source config-zip` |
| **CI/CD**                | Manual or custom scripts (complex)         | GitHub Actions / Azure DevOps (standard)   |
| **Testing**              | BizTalk Unit Test Framework (limited)      | xUnit + Moq (standard .NET)               |
| **Observability**        | BizTalk Admin Group Hub + SQL queries      | Application Insights + Log Analytics       |
| **Source Control**       | Binary .btm, .odx files (poor diffability) | Plain C# text files                        |
| **Scalability**          | Manual (add BizTalk servers to group)      | Automatic (0 to N instances instantly)     |
| **Cost Model**           | Per-server license + VM + SQL              | Per-execution (~$0.20/1M)                  |

---

## Non-Functional Requirements Comparison

### Latency

| Scenario                        | BizTalk                         | Azure Functions                   |
|---------------------------------|---------------------------------|-----------------------------------|
| Message processing time         | 200–800ms (MessageBox overhead) | 50–200ms (in-process, cold start excluded) |
| Cold start (first request)      | N/A (always warm)               | 1–3 seconds (Consumption plan)    |
| Warm throughput                 | ~200 msg/sec per server         | 1,000+ req/sec auto-scaled        |

> **Note:** For latency-sensitive scenarios requiring sub-100ms consistently, consider Premium or Dedicated App Service plan for Azure Functions to eliminate cold starts.

### Throughput

| Scenario                        | BizTalk                         | Azure Functions                   |
|---------------------------------|---------------------------------|-----------------------------------|
| Peak throughput                 | Limited by MessageBox SQL IOPS  | Auto-scales horizontally          |
| Burst handling                  | Queue-backed (handles smoothly) | Burst to 200 instances in seconds |
| Backpressure                    | MessageBox queuing              | Configure max instance count      |

### High Availability

| Dimension                       | BizTalk                                    | Azure Functions                            |
|---------------------------------|--------------------------------------------|--------------------------------------------|
| Architecture                    | BizTalk Server Group (active-active)       | Azure platform-managed HA                 |
| SLA                             | ~99.9% (with clustering)                   | 99.95% (Consumption), 99.99% (Premium)    |
| Failover                        | Manual or NLB-based                        | Automatic (Azure infrastructure)           |
| Disaster recovery               | SQL Always On + BizTalk group replication  | Azure Paired Regions + Traffic Manager     |
| Maintenance windows             | Required for updates                       | Zero-downtime rolling updates             |
