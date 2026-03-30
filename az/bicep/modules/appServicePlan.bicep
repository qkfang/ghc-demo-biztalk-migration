@description('Consumption-based App Service Plan for Azure Functions')
param location string
param env string
param project string

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: 'asp-${project}-${env}'
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {}
  tags: {
    env: env
    project: project
  }
}

output appServicePlanId string = appServicePlan.id
