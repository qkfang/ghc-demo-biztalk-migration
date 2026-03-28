# Azure Functions – SuperFundManagementFunc

Cloud-native replacement for the BizTalk `SuperContributionOrchestration`, built on **Azure Functions v4** with **.NET 8 isolated worker**. This function processes superannuation contribution requests from employer payroll systems and routes fund allocation instructions to the fund administration platform.

## Project Structure

```
func/
├── SuperFundManagementFunc.sln
├── SuperFundManagementFunc/
│   ├── SuperFundManagementFunc.csproj
│   ├── Program.cs                             # Host builder + DI
│   ├── host.json
│   ├── local.settings.json
│   ├── Models/
│   │   ├── SuperContribution.cs               # Deserializes incoming XML
│   │   └── FundAllocation.cs                  # Serializes outgoing XML
│   ├── Services/
│   │   ├── IContributionTransformService.cs
│   │   ├── ContributionTransformService.cs    # Replaces ContributionToAllocationMap.btm
│   │   ├── IFundAllocationSenderService.cs
│   │   └── FundAllocationSenderService.cs     # Replaces AllocationHttpSend port
│   └── Functions/
│       └── SuperContributionFunction.cs       # HTTP trigger – replaces orchestration
└── SuperFundManagementFunc.Tests/
    ├── SuperFundManagementFunc.Tests.csproj
    ├── Services/
    │   ├── ContributionTransformServiceTests.cs
    │   └── FundAllocationSenderServiceTests.cs
    └── Functions/
        └── SuperContributionFunctionTests.cs
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) (local storage emulator) or a real Azure Storage account

## Run Locally

1. **Start Azurite** (for local storage):
   ```bash
   azurite --silent &
   ```

2. **Run the function app**:
   ```bash
   cd func/SuperFundManagementFunc
   func start
   ```
   The function will listen at `http://localhost:7071/api/contributions`.

3. **Test with curl**:
   ```bash
   curl -X POST http://localhost:7071/api/contributions \
     -H "Content-Type: application/xml" \
     -d @../../docs/sample-contribution.xml
   ```

   **Sample XML payload** (`sample-contribution.xml`):
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <SuperContributionRequest xmlns="http://SuperFundManagement.Schemas.SuperContribution">
     <ContributionId>CONT-2024-001</ContributionId>
     <EmployerId>EMP-001</EmployerId>
     <EmployerName>Acme Corporation Pty Ltd</EmployerName>
     <EmployerABN>51824753556</EmployerABN>
     <PayPeriodEndDate>2024-06-30</PayPeriodEndDate>
     <Members>
       <Member>
         <MemberAccountNumber>SF-100001</MemberAccountNumber>
         <MemberName>Jane Smith</MemberName>
         <ContributionType>SuperannuationGuarantee</ContributionType>
         <GrossAmount>875.00</GrossAmount>
       </Member>
       <Member>
         <MemberAccountNumber>SF-100002</MemberAccountNumber>
         <MemberName>John Citizen</MemberName>
         <ContributionType>SuperannuationGuarantee</ContributionType>
         <GrossAmount>750.00</GrossAmount>
       </Member>
     </Members>
     <TotalContribution>1625.00</TotalContribution>
     <Currency>AUD</Currency>
     <PaymentReference>PAY-REF-20240630</PaymentReference>
   </SuperContributionRequest>
   ```

   **Expected response (202 Accepted)**:
   ```json
   {
     "allocationId": "FA-CONT-2024-001",
     "sourceContributionRef": "CONT-2024-001",
     "status": "PENDING"
   }
   ```

## Run Tests

```bash
cd func
dotnet test SuperFundManagementFunc.sln --verbosity normal
```

With code coverage:
```bash
dotnet test SuperFundManagementFunc.sln \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage
```

## Environment Variables

| Variable                | Description                                           | Default (local)                              |
|-------------------------|-------------------------------------------------------|----------------------------------------------|
| `FundAdminPlatformUrl`  | URL of the downstream fund administration platform    | `http://localhost:7072/api/mock-allocations` |
| `AzureWebJobsStorage`   | Azure Storage connection string                       | `UseDevelopmentStorage=true`                 |

## HTTP API

### POST /api/contributions

Accepts an XML `SuperContributionRequest` and returns a fund allocation confirmation.

**Request:**
- Method: `POST`
- Content-Type: `application/xml`
- Body: XML conforming to `SuperContributionSchema.xsd`

**Responses:**

| Status | Meaning                                                        |
|--------|----------------------------------------------------------------|
| `202`  | Contribution accepted and allocation instruction forwarded     |
| `400`  | Validation failed (missing ContributionId, no members, etc.)  |
| `502`  | Fund administration platform unreachable or returned an error |
| `500`  | Internal transformation error                                  |

## Contribution Types

| Value                    | Description                                           |
|--------------------------|-------------------------------------------------------|
| `SuperannuationGuarantee`| Mandatory employer contributions (SG rate)           |
| `VoluntaryEmployee`      | Member voluntary after-tax contributions              |
| `VoluntaryEmployer`      | Additional employer contributions above SG            |

## Dependency Injection

```
SuperContributionFunction
  ├── IContributionTransformService  → ContributionTransformService
  └── IFundAllocationSenderService   → FundAllocationSenderService
         └── IHttpClientFactory (registered via AddHttpClient())
```

## Configuration for Production

In Azure, set the following Application Settings:
```
FundAdminPlatformUrl = https://your-fund-admin-platform/api/allocations
```
