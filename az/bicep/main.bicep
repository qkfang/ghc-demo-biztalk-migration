targetScope = 'resourceGroup'

@description('Environment name (dev, prod)')
param env string

@description('Project name used for resource naming')
param project string

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('URL of the Fund Admin API')
param fundAdminApiUrl string

var storageAccountName = '${toLower(project)}${toLower(env)}sa'
var appServicePlanName = '${project}-${env}-asp'
var appInsightsName = '${project}-${env}-ai'
var logAnalyticsWorkspaceName = '${project}-${env}-law'
var functionAppName = '${project}-${env}-func'

module storage './modules/storage.bicep' = {
  name: 'storage'
  params: {
    location: location
    project: project
    env: env
    storageAccountName: storageAccountName
  }
}

module appServicePlan './modules/appServicePlan.bicep' = {
  name: 'appServicePlan'
  params: {
    location: location
    project: project
    env: env
    appServicePlanName: appServicePlanName
  }
}

module appInsights './modules/appInsights.bicep' = {
  name: 'appInsights'
  params: {
    location: location
    project: project
    env: env
    appInsightsName: appInsightsName
    logAnalyticsWorkspaceName: logAnalyticsWorkspaceName
  }
}

module functionApp './modules/functionApp.bicep' = {
  name: 'functionApp'
  params: {
    location: location
    project: project
    env: env
    functionAppName: functionAppName
    appServicePlanId: appServicePlan.outputs.appServicePlanId
    storageConnectionString: storage.outputs.storageConnectionString
    appInsightsConnectionString: appInsights.outputs.appInsightsConnectionString
    fundAdminApiUrl: fundAdminApiUrl
  }
}

output functionAppName string = functionApp.outputs.functionAppName
output functionAppPrincipalId string = functionApp.outputs.functionAppPrincipalId
