// App Service Plan for Azure Functions
@description('Azure region')
param location string

@description('App Service Plan name')
param appServicePlanName string

@description('Resource tags')
param tags object

@description('SKU name (Y1 for Consumption, EP1/EP2/EP3 for Premium)')
param skuName string = 'Y1'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuName == 'Y1' ? 'Dynamic' : 'ElasticPremium'
  }
  properties: {
    reserved: false // Set to true for Linux
  }
}

output planId string = appServicePlan.id
output planName string = appServicePlan.name
