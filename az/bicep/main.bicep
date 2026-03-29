@description('SuperFundManagement Azure Functions infrastructure')
param location string = resourceGroup().location
param env string
param project string = 'superfund'
param fundAdminApiUrl string

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    location: location
    env: env
    project: project
  }
}

module appServicePlan 'modules/appServicePlan.bicep' = {
  name: 'appServicePlan'
  params: {
    location: location
    env: env
    project: project
  }
}

module appInsights 'modules/appInsights.bicep' = {
  name: 'appInsights'
  params: {
    location: location
    env: env
    project: project
  }
}

module functionApp 'modules/functionApp.bicep' = {
  name: 'functionApp'
  params: {
    location: location
    env: env
    project: project
    appServicePlanId: appServicePlan.outputs.appServicePlanId
    storageConnectionString: storage.outputs.storageConnectionString
    appInsightsConnectionString: appInsights.outputs.appInsightsConnectionString
    fundAdminApiUrl: fundAdminApiUrl
  }
}

output functionAppName string = functionApp.outputs.functionAppName
output functionAppHostname string = functionApp.outputs.functionAppHostname
