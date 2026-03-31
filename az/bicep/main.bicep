// Main deployment template for SuperFund Function App infrastructure
// This replaces BizTalk Server infrastructure with Azure PaaS services

@description('Azure region for resources')
param location string = resourceGroup().location

@description('Environment name (dev, test, prod)')
@maxLength(4)
param environmentName string = 'dev'

@description('Application name prefix')
param appName string = 'superfund'

@description('Fund Admin API endpoint URL')
param fundAdminApiUrl string

@description('Tags to apply to all resources')
param tags object = {
  Application: 'SuperFund'
  Environment: environmentName
  ManagedBy: 'Bicep'
}

// Generate unique resource names
var uniqueSuffix = uniqueString(resourceGroup().id)
var storageAccountName = '${appName}${environmentName}${uniqueSuffix}'
var appServicePlanName = '${appName}-asp-${environmentName}'
var functionAppName = '${appName}-func-${environmentName}'
var appInsightsName = '${appName}-ai-${environmentName}'
var logAnalyticsName = '${appName}-la-${environmentName}'

// Storage Account for Azure Functions
module storage 'modules/storage.bicep' = {
  name: 'storage-deployment'
  params: {
    location: location
    storageAccountName: storageAccountName
    tags: tags
  }
}

// Log Analytics Workspace for Application Insights
module logAnalytics 'modules/log-analytics.bicep' = {
  name: 'log-analytics-deployment'
  params: {
    location: location
    logAnalyticsName: logAnalyticsName
    tags: tags
  }
}

// Application Insights for monitoring and diagnostics
module appInsights 'modules/app-insights.bicep' = {
  name: 'app-insights-deployment'
  params: {
    location: location
    appInsightsName: appInsightsName
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
    tags: tags
  }
}

// App Service Plan (Consumption or Premium)
module appServicePlan 'modules/app-service-plan.bicep' = {
  name: 'app-service-plan-deployment'
  params: {
    location: location
    appServicePlanName: appServicePlanName
    tags: tags
  }
}

// Azure Function App
module functionApp 'modules/function-app.bicep' = {
  name: 'function-app-deployment'
  params: {
    location: location
    functionAppName: functionAppName
    appServicePlanId: appServicePlan.outputs.planId
    storageAccountName: storage.outputs.storageAccountName
    storageAccountKey: storage.outputs.storageAccountKey
    appInsightsConnectionString: appInsights.outputs.connectionString
    appInsightsInstrumentationKey: appInsights.outputs.instrumentationKey
    fundAdminApiUrl: fundAdminApiUrl
    tags: tags
  }
}

// Outputs
output functionAppName string = functionApp.outputs.functionAppName
output functionAppUrl string = functionApp.outputs.functionAppUrl
output storageAccountName string = storage.outputs.storageAccountName
output appInsightsName string = appInsights.outputs.appInsightsName
