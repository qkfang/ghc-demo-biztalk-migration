using './main.bicep'

param environment = 'dev'
param projectName = 'order-processing'
param fulfillmentServiceUrl = 'https://downstream-service/api/fulfillment'
param location = 'australiaeast'
