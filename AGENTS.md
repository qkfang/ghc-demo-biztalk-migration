# AGENTS.md

## Repository 

Migrating a legacy BizTalk Server 2020 integration to Azure Functions v4 (.NET 8).

## Scenario

A Superannuation Fund Management integration that:
1. Receives an HTTP POST with an XML `SuperContributionRequest`
2. Transforms it to a `FundAllocationInstruction` format
3. Forwards the result to a downstream fund administration API

## Folder Structure

| Folder | Contents |
|--------|----------|
| `app-biztalk/` | Original BizTalk Server 2020 solution (schemas, maps, orchestrations) |
| `app-fundadmin/` | Mock downstream fund administration API (.NET 8) |
| `az/funcapp/` | Migrated Azure Functions v4 app + xUnit tests (generated) |
| `az/bicep/` | Bicep infrastructure templates (generated) |
| `az/scripts/` | Local test scripts |
| `docs/` | Architecture docs, migration plan, demo script |

## Migration Output

When migrating, generate files into:
- `az/funcapp/` — C# .NET 8 isolated worker Azure Functions app and xUnit tests
- `az/bicep/` — Bicep modules for storage, app service plan, app insights, and function app

## Key BizTalk Artifacts

| File | Purpose |
|------|---------|
| `app-biztalk/SuperFundManagement/Schemas/SuperContributionSchema.xsd` | Input message schema |
| `app-biztalk/SuperFundManagement/Schemas/FundAllocationSchema.xsd` | Output message schema |
| `app-biztalk/SuperFundManagement/Maps/ContributionToAllocationMap.btm` | Transformation map |
| `app-biztalk/SuperFundManagement/Maps/ContributionToAllocationMap.xsl` | XSLT for the map |
| `app-biztalk/SuperFundManagement/Orchestrations/SuperContributionOrchestration.odx` | Orchestration flow |
| `app-biztalk/SuperFundManagement/BindingFile.xml` | Endpoint configuration |

## Agent Instructions

- Always read all BizTalk source artifacts before generating any output
- Follow the BizTalk Migration Agent mode instructions for project structure and code conventions
- Target: Azure Functions v4, .NET 8 isolated worker
- Do not add features beyond what the original BizTalk solution does
