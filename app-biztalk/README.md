# BizTalk SuperFundManagement Solution

This folder contains the BizTalk Server 2020 solution for the **Superannuation Fund Management** application — the legacy baseline for the migration demo. It models a super company processing employer contribution requests and routing fund allocation instructions to the fund administration platform.

## Solution Structure

```
SuperFundManagement/
├── SuperFundManagement.btproj         # BizTalk project file
├── BindingFile.xml                    # Port & orchestration bindings
├── Schemas/
│   ├── SuperContributionSchema.xsd    # Incoming employer contribution schema
│   └── FundAllocationSchema.xsd       # Outgoing fund allocation schema
├── Maps/
│   └── ContributionToAllocationMap.btm  # XSLT map with functoids
├── Orchestrations/
│   └── SuperContributionOrchestration.odx  # Main orchestration
└── Pipelines/
    ├── HttpReceivePipeline.btp        # Receive pipeline (XML Disassembler)
    └── HttpSendPipeline.btp           # Send pipeline (XML Assembler)
```

## Prerequisites

- **BizTalk Server 2020** (Developer or Enterprise Edition)
- **Visual Studio 2019/2022** with BizTalk extensions installed
- **BizTalk Server 2020 Developer Tools** (included with BizTalk installation)
- SQL Server 2016+ (for BizTalk Management DB)
- IIS configured for HTTP adapter

## Build

1. Open `SuperFundManagement.sln` in Visual Studio
2. Right-click the solution → **Restore NuGet Packages**
3. Set the `BizTalkInstallPath` property if not set automatically:
   ```xml
   <!-- In project properties or Directory.Build.props -->
   <BizTalkInstallPath>C:\Program Files (x86)\Microsoft BizTalk Server 2020</BizTalkInstallPath>
   ```
4. Build the solution:
   ```
   msbuild SuperFundManagement.sln /p:Configuration=Release
   ```

## Deploy to BizTalk Server

### Option 1: Deploy from Visual Studio

1. In Solution Explorer, right-click `SuperFundManagement` project
2. Select **Deploy**
3. The project auto-deploys to the BizTalk Management Database

### Option 2: Deploy via BTSTask

```powershell
# Import the MSI (after building)
BTSTask ImportApp /Package:"bin\Release\SuperFundManagement.msi" /Overwrite

# Import bindings
BTSTask ImportBindings /ApplicationName:SuperFundManagement /Source:BindingFile.xml

# Enlist and start
BTSTask StartApplication /ApplicationName:SuperFundManagement
```

### Option 3: Deploy via BizTalk Admin Console

1. Open **BizTalk Server Administration Console**
2. Right-click **Applications** → **Import** → **MSI file**
3. Follow the import wizard
4. After import, right-click application → **Import Bindings**
5. Select `BindingFile.xml`
6. Start the application

## Port Configurations

### Receive Location: `ContributionHttpReceive_Location`

| Property        | Value                                 |
|-----------------|---------------------------------------|
| Transport       | HTTP                                  |
| URL             | `/SuperFundManagement/Receive`        |
| Port            | 7070 (IIS binding)                    |
| Pipeline        | `HttpReceivePipeline`                 |
| Message Type    | `SuperContribution.SuperContributionRequest` |
| Authentication  | None (extend for production)          |

The HTTP adapter is hosted in IIS. Configure an IIS application pointing to `%BTSHTTPRECEIVE%` virtual directory on port 7070.

### Send Port: `AllocationHttpSend`

| Property        | Value                                              |
|-----------------|----------------------------------------------------|
| Transport       | HTTP                                               |
| URL             | `http://fund-admin-platform/api/allocations`       |
| Pipeline        | `HttpSendPipeline`                                 |
| Content-Type    | `application/xml`                                  |
| Retry Count     | 3                                                  |
| Retry Interval  | 5 seconds                                          |
| Map             | `ContributionToAllocationMap`                      |
| Filter          | `BTS.ReceivePortName == ContributionHttpReceive`   |

## Orchestration Flow

```
HTTP POST (XML SuperContributionRequest)
        ↓
[ContributionHttpReceive] Receive Port
        ↓
[HttpReceivePipeline] XML Disassembler + Validator
        ↓
[SuperContributionOrchestration]
    1. ReceiveContribution (activate)
    2. Construct FundAllocationInstructionMsg via ContributionToAllocationMap
    3. SendFundAllocationInstruction
        ↓
[HttpSendPipeline] XML Assembler
        ↓
[AllocationHttpSend] Send Port
        ↓
HTTP POST to fund-admin-platform/api/allocations
```

## Map: ContributionToAllocationMap

Key transformations:

| Source Field                       | Target Field                              | Logic                                       |
|------------------------------------|-------------------------------------------|---------------------------------------------|
| `ContributionId`                   | `AllocationId`                            | String Concatenate: `"FA-"` + ContributionId |
| `ContributionId`                   | `SourceContributionRef`                   | Direct copy                                 |
| `EmployerId`                       | `EmployerDetails/EmployerId`              | Direct copy                                 |
| `EmployerName`                     | `EmployerDetails/EmployerName`            | Direct copy                                 |
| `EmployerABN`                      | `EmployerDetails/ABN`                     | Direct copy                                 |
| `PayPeriodEndDate`                 | `AllocationDate`                          | Direct copy                                 |
| `Members/Member[*]`                | `MemberAllocations/Allocation[*]`         | Looping functoid                            |
| `Member/MemberAccountNumber`       | `Allocation/AccountNumber`                | Direct copy                                 |
| `Member/MemberName`                | `Allocation/MemberName`                   | Direct copy                                 |
| `Member/ContributionType`          | `Allocation/ContributionType`             | Direct copy                                 |
| `Member/GrossAmount`               | `Allocation/ContributionAmount`           | Direct copy                                 |
| *(constant)*                       | `Allocation/AllocationStatus`             | Value: `"PENDING"`                          |
| `TotalContribution`                | `TotalAllocated`                          | Direct copy                                 |
| `Currency`                         | `CurrencyCode`                            | Direct copy                                 |
| *(constant)*                       | `Status`                                  | Value: `"PENDING"`                          |

## Troubleshooting

- **Pipeline validation failure**: Ensure `SuperContributionSchema.xsd` is deployed to the BizTalk Management DB
- **HTTP 404 on receive**: Verify IIS virtual directory for HTTP adapter is running on port 7070
- **Orchestration suspended**: Check BizTalk Admin Console → Group Hub → Suspended Instances
- **Map failures**: Use BizTalk Mapper in Visual Studio to test the `.btm` file with sample XML
