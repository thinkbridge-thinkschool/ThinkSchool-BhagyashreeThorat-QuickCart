// QuickCart — infrastructure orchestrator.
// Deployed at resource-group scope; wires SQL + Service Bus into the API's settings.
//   az deployment group what-if -g rg-quickcart-dev -f infra/main.bicep -p infra/main.dev.bicepparam

targetScope = 'resourceGroup'

@description('Short environment name, e.g. dev or prod. Used to build resource names.')
@allowed([
  'dev'
  'prod'
])
param environmentName string

@description('Azure region for all resources. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('SQL administrator login.')
param sqlAdministratorLogin string

@description('SQL administrator password.')
@secure()
param sqlAdministratorLoginPassword string

// --- SKUs (overridden per environment via .bicepparam) ---
@description('App Service Plan SKU.')
param appServicePlanSku string = 'B1'

@description('SQL database SKU name.')
param sqlDatabaseSkuName string = 'Basic'

@description('SQL database SKU tier.')
param sqlDatabaseSkuTier string = 'Basic'

@description('Service Bus namespace SKU.')
param serviceBusSku string = 'Standard'

// --- Naming ---
// A short suffix keeps globally-unique names (web app, SQL server, SB namespace) collision-free.
var suffix = uniqueString(resourceGroup().id)
var namePrefix = 'quickcart-${environmentName}'

var tags = {
  application: 'QuickCart'
  environment: environmentName
  managedBy: 'bicep'
}

module serviceBus 'modules/servicebus.bicep' = {
  name: 'serviceBus'
  params: {
    namespaceName: 'sb-${namePrefix}-${suffix}'
    location: location
    skuName: serviceBusSku
    queueName: 'order-events'
    tags: tags
  }
}

module sql 'modules/sql.bicep' = {
  name: 'sql'
  params: {
    serverName: 'sql-${namePrefix}-${suffix}'
    databaseName: 'quickcart'
    location: location
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorLoginPassword
    databaseSkuName: sqlDatabaseSkuName
    databaseSkuTier: sqlDatabaseSkuTier
    tags: tags
  }
}

module appService 'modules/appservice.bicep' = {
  name: 'appService'
  params: {
    planName: 'plan-${namePrefix}'
    webAppName: 'app-${namePrefix}-${suffix}'
    location: location
    skuName: appServicePlanSku
    sqlConnectionString: sql.outputs.connectionString
    serviceBusConnectionString: serviceBus.outputs.connectionString
    aspNetCoreEnvironment: environmentName == 'prod' ? 'Production' : 'Development'
    tags: tags
  }
}

@description('Public URL of the deployed API.')
output apiUrl string = 'https://${appService.outputs.defaultHostName}'

@description('SQL server FQDN.')
output sqlServerFqdn string = sql.outputs.fullyQualifiedDomainName

@description('Service Bus namespace id.')
output serviceBusNamespaceId string = serviceBus.outputs.namespaceId
