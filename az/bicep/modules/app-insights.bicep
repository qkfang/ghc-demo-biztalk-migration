// Application Insights for monitoring and telemetry
@description('Azure region')
param location string

@description('Application Insights name')
param appInsightsName string

@description('Log Analytics Workspace ID')
param logAnalyticsWorkspaceId string

@description('Resource tags')
param tags object

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspaceId
  }
}

output appInsightsName string = appInsights.name
output instrumentationKey string = appInsights.properties.InstrumentationKey
output connectionString string = appInsights.properties.ConnectionString
