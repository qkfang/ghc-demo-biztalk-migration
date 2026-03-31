# Azure Infrastructure Deployment

This directory contains Bicep templates for deploying the SuperFund Function App infrastructure to Azure.

## Architecture

The infrastructure includes:

- **Storage Account**: Required for Azure Functions runtime state and triggers
- **Log Analytics Workspace**: Centralized logging and monitoring
- **Application Insights**: Application telemetry and diagnostics
- **App Service Plan**: Hosting plan for the Function App (Consumption tier by default)
- **Function App**: Hosts the migrated BizTalk integration logic

## Prerequisites

- Azure CLI installed and configured
- Azure subscription with appropriate permissions
- Resource group created in Azure

## Deployment

### Create Resource Group

```bash
az group create --name rg-superfund-dev --location australiaeast
```

### Deploy Infrastructure

```bash
az deployment group create \
  --resource-group rg-superfund-dev \
  --template-file main.bicep \
  --parameters main.parameters.dev.json
```

### Deploy with Custom Parameters

```bash
az deployment group create \
  --resource-group rg-superfund-dev \
  --template-file main.bicep \
  --parameters \
    environmentName=dev \
    appName=superfund \
    fundAdminApiUrl=https://your-fund-admin-api.com/api/allocations
```

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `location` | Azure region | Resource group location |
| `environmentName` | Environment name (dev/test/prod) | dev |
| `appName` | Application name prefix | superfund |
| `fundAdminApiUrl` | Fund Admin API endpoint | (required) |
| `tags` | Resource tags | See parameters file |

## Outputs

After deployment, the following outputs are available:

- `functionAppName`: Name of the deployed Function App
- `functionAppUrl`: URL of the Function App
- `storageAccountName`: Name of the storage account
- `appInsightsName`: Name of the Application Insights instance

## Clean Up

To delete all resources:

```bash
az group delete --name rg-superfund-dev --yes
```

## Migration Notes

This Bicep infrastructure replaces the following BizTalk Server components:

- **BizTalk Server**: Replaced by Azure Functions
- **IIS**: Replaced by App Service Plan
- **SQL Server (BizTalk databases)**: Not required - serverless architecture
- **BizTalk Management Database**: Replaced by Azure Storage
- **BAM Database**: Replaced by Application Insights

## Cost Optimization

For production workloads, consider:

- Premium App Service Plan for VNet integration and better performance
- Zone redundancy for high availability
- Reserved instances for cost savings
