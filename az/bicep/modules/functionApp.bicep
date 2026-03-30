@description('Azure Functions v4 app (.NET 8 isolated worker)')
param location string
param env string
param project string
param appServicePlanId string
param storageConnectionString string
param appInsightsConnectionString string
param fundAdminApiUrl string

resource functionApp 'Microsoft.Web/sites@2023-01-01' = {
  name: 'func-${project}-${env}'
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanId
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: storageConnectionString
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: storageConnectionString
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower('func-${project}-${env}')
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'FundAdminApiUrl'
          value: fundAdminApiUrl
        }
      ]
      netFrameworkVersion: 'v8.0'
    }
    httpsOnly: true
  }
  tags: {
    env: env
    project: project
  }
}

output functionAppName string = functionApp.name
output functionAppHostname string = functionApp.properties.defaultHostName
output principalId string = functionApp.identity.principalId
