@description('Deployment environment (dev, staging, prod)')
param environment string

@description('Short project name used in resource naming')
param projectName string

@description('URL of the downstream fulfillment service')
param fulfillmentServiceUrl string

@description('Azure region for all resources')
param location string = resourceGroup().location

// ─────────────────────────────────────────────────────────────────────────────
// Resource name tokens
// ─────────────────────────────────────────────────────────────────────────────
var resourceToken = toLower('${projectName}${environment}')
var tags = {
  environment: environment
  project: projectName
  managedBy: 'bicep'
}

// ─────────────────────────────────────────────────────────────────────────────
// Modules
// ─────────────────────────────────────────────────────────────────────────────
module storage 'modules/storage.bicep' = {
  name: 'storage-${resourceToken}'
  params: {
    storageAccountName: 'st${resourceToken}'
    location: location
    tags: tags
  }
}

module appServicePlan 'modules/appServicePlan.bicep' = {
  name: 'plan-${resourceToken}'
  params: {
    planName: 'asp-${resourceToken}'
    location: location
    tags: tags
  }
}

module appInsights 'modules/appInsights.bicep' = {
  name: 'insights-${resourceToken}'
  params: {
    appInsightsName: 'appi-${resourceToken}'
    logAnalyticsWorkspaceName: 'log-${resourceToken}'
    location: location
    tags: tags
  }
}

module functionApp 'modules/functionApp.bicep' = {
  name: 'func-${resourceToken}'
  params: {
    functionAppName: 'func-${resourceToken}'
    location: location
    tags: tags
    planId: appServicePlan.outputs.planId
    storageConnectionString: storage.outputs.connectionString
    appInsightsInstrumentationKey: appInsights.outputs.instrumentationKey
    appInsightsConnectionString: appInsights.outputs.connectionString
    fulfillmentServiceUrl: fulfillmentServiceUrl
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Outputs
// ─────────────────────────────────────────────────────────────────────────────
output functionAppName string = functionApp.outputs.functionAppName
output functionAppHostName string = functionApp.outputs.defaultHostName
output storageAccountName string = storage.outputs.accountName
