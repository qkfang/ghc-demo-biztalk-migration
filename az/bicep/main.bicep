targetScope = 'resourceGroup'

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('Environment name (dev, test, prod)')
param environmentName string = 'dev'

@description('Application name used in resource naming')
param appName string = 'superfundmgmt'

@description('Fund admin API URL for the function app setting')
param fundAdminApiUrl string = 'https://fund-admin-platform/api/allocations'

var tags = {
  env:         environmentName
  project:     appName
  managedBy:   'bicep'
}

var storageAccountName         = 'st${appName}${environmentName}'
var appServicePlanName         = 'asp-${appName}-${environmentName}'
var appInsightsName            = 'appi-${appName}-${environmentName}'
var logAnalyticsWorkspaceName  = 'law-${appName}-${environmentName}'
var functionAppName            = 'func-${appName}-${environmentName}'

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    location:           location
    storageAccountName: storageAccountName
    tags:               tags
  }
}

module appServicePlan 'modules/appServicePlan.bicep' = {
  name: 'appServicePlan'
  params: {
    location: location
    planName: appServicePlanName
    tags:     tags
  }
}

module appInsights 'modules/appInsights.bicep' = {
  name: 'appInsights'
  params: {
    location:                    location
    appInsightsName:             appInsightsName
    logAnalyticsWorkspaceName:   logAnalyticsWorkspaceName
    tags:                        tags
  }
}

module functionApp 'modules/functionApp.bicep' = {
  name: 'functionApp'
  params: {
    location:                   location
    functionAppName:            functionAppName
    planId:                     appServicePlan.outputs.planId
    storageAccountName:         storage.outputs.storageAccountName
    appInsightsConnectionString: appInsights.outputs.appInsightsConnectionString
    fundAdminApiUrl:            fundAdminApiUrl
    tags:                       tags
  }
}

output functionAppName            string = functionApp.outputs.functionAppName
output functionAppDefaultHostName string = functionApp.outputs.functionAppDefaultHostName
output functionAppPrincipalId     string = functionApp.outputs.functionAppPrincipalId
