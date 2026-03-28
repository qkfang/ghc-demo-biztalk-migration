# Bicep IaC – Order Processing Function App

This folder contains the Azure Bicep Infrastructure-as-Code for deploying the `OrderProcessingFunc` Azure Function and its supporting resources.

## Resources Deployed

| Resource                  | Type                                      | SKU / Tier               |
|---------------------------|-------------------------------------------|--------------------------|
| Storage Account           | `Microsoft.Storage/storageAccounts`       | Standard_LRS             |
| App Service Plan          | `Microsoft.Web/serverfarms`               | Y1 (Consumption/Dynamic) |
| Log Analytics Workspace   | `Microsoft.OperationalInsights/workspaces`| PerGB2018                |
| Application Insights      | `Microsoft.Insights/components`           | Workspace-based          |
| Function App              | `Microsoft.Web/sites`                     | dotnet-isolated / .NET 8 |

## Parameters

| Parameter             | Description                                    | Default (in `.bicepparam`) |
|-----------------------|------------------------------------------------|---------------------------|
| `environment`         | Deployment environment (`dev`, `staging`, `prod`) | `dev`               |
| `projectName`         | Short project name used in resource naming     | `order-processing`        |
| `fulfillmentServiceUrl` | URL of the downstream HTTP endpoint          | `https://downstream-service/api/fulfillment` |
| `location`            | Azure region                                   | `australiaeast`           |

## Naming Convention

Resources are named using the pattern `{type-prefix}-{projectName}{environment}`, e.g.:
- Function App: `func-order-processingdev`
- Storage: `storder-processingdev`
- App Insights: `appi-order-processingdev`

## Prerequisites

- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) ≥ 2.50
- [Bicep CLI](https://learn.microsoft.com/azure/azure-resource-manager/bicep/install) ≥ 0.24
  ```bash
  az bicep install
  az bicep upgrade
  ```
- An Azure subscription with Contributor rights on a resource group

## Deploy

### 1. Login and set subscription

```bash
az login
az account set --subscription "<your-subscription-id>"
```

### 2. Create resource group

```bash
az group create \
  --name rg-order-processing-dev \
  --location australiaeast
```

### 3. Validate the Bicep template

```bash
az deployment group validate \
  --resource-group rg-order-processing-dev \
  --template-file bicep/main.bicep \
  --parameters bicep/main.bicepparam
```

### 4. Preview changes (what-if)

```bash
az deployment group what-if \
  --resource-group rg-order-processing-dev \
  --template-file bicep/main.bicep \
  --parameters bicep/main.bicepparam
```

### 5. Deploy

```bash
az deployment group create \
  --resource-group rg-order-processing-dev \
  --template-file bicep/main.bicep \
  --parameters bicep/main.bicepparam \
  --name "order-processing-$(date +%Y%m%d%H%M%S)"
```

### 6. Deploy function code

After infrastructure is provisioned, publish the function app:

```bash
cd func/OrderProcessingFunc
dotnet publish -c Release -o ./publish

# Zip and deploy
cd publish
zip -r ../deploy.zip .
az functionapp deployment source config-zip \
  --resource-group rg-order-processing-dev \
  --name func-order-processingdev \
  --src ../deploy.zip
```

## Override Parameters at Deploy Time

You can override individual parameters without editing the `.bicepparam` file:

```bash
az deployment group create \
  --resource-group rg-order-processing-prod \
  --template-file bicep/main.bicep \
  --parameters bicep/main.bicepparam \
             environment=prod \
             fulfillmentServiceUrl=https://prod-service/api/fulfillment
```

## Tear Down

```bash
az group delete --name rg-order-processing-dev --yes --no-wait
```
