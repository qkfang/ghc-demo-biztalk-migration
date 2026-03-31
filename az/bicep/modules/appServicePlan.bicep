param location string
param project string
param env string
param appServicePlanName string

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  kind: 'functionapp'
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  tags: {
    env: env
    project: project
  }
}

output appServicePlanId string = appServicePlan.id
