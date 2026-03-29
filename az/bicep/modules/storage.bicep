@description('Storage Account for Azure Functions runtime')
param location string
param env string
param project string

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: 'st${toLower(project)}${toLower(env)}'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  tags: {
    env: env
    project: project
  }
}

output storageAccountName string = storageAccount.name
output storageConnectionString string = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
