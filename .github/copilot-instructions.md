# GitHub Copilot Instructions

## Repository Purpose

This repository migrates a legacy **BizTalk Server 2020** Superannuation Fund Management integration to **Azure Functions v4 (.NET 8 isolated worker)**.

The integration:
1. Receives an HTTP POST with an XML `SuperContributionRequest`
2. Transforms it to a `FundAllocationInstruction` format
3. Forwards the result to a downstream fund administration API

---

## Folder Structure

| Folder | Contents |
|--------|----------|
| `app-biztalk/` | Original BizTalk Server 2020 solution — schemas, maps, orchestrations, pipelines |
| `app-fundadmin/` | Mock downstream fund administration API (.NET 8 minimal API) |
| `az/funcapp/` | Migrated Azure Functions v4 app + xUnit tests |
| `az/bicep/` | Bicep IaC modules for Function App, App Service Plan, App Insights, Storage |
| `az/scripts/` | Local PowerShell test scripts |
| `docs/` | Architecture diagrams, migration plan, demo script |

---

## Rules

- Do **not** add features beyond what the original BizTalk solution does
- Do **not** use XSLT at runtime in the Azure Functions app — use C# transformation only
- Keep the documentation and logics simple and straightforward for demo purposes
- Keep everything simple for business user to underatnd the value of github copilot
