param location string
param functionAppName string
param planId string
param storageAccountName string
param appInsightsConnectionString string
param fundAdminApiUrl string
param tags object = {}

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' existing = {
  name: storageAccountName
}

var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'

resource functionApp 'Microsoft.Web/sites@2023-01-01' = {
  name: functionAppName
  location: location
  tags: tags
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: planId
    siteConfig: {
      appSettings: [
        { name: 'AzureWebJobsStorage',                  value: storageConnectionString }
        { name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING', value: storageConnectionString }
        { name: 'WEBSITE_CONTENTSHARE',                 value: toLower(functionAppName) }
        { name: 'FUNCTIONS_EXTENSION_VERSION',          value: '~4' }
        { name: 'FUNCTIONS_WORKER_RUNTIME',             value: 'dotnet-isolated' }
        { name: 'WEBSITE_RUN_FROM_PACKAGE',             value: '1' }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING',value: appInsightsConnectionString }
        { name: 'DOTNET_VERSION',                       value: '8.0' }
        { name: 'FundAdminApiUrl',                      value: fundAdminApiUrl }
      ]
      netFrameworkVersion: 'v8.0'
      use32BitWorkerProcess: false
    }
    httpsOnly: true
  }
}

output functionAppId                string = functionApp.id
output functionAppName              string = functionApp.name
output functionAppPrincipalId       string = functionApp.identity.principalId
output functionAppDefaultHostName   string = functionApp.properties.defaultHostName
