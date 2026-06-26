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

@description('Entra admin login (UPN/email) for the SQL server.')
param aadAdminLogin string

@description('Entra admin object id for the SQL server.')
param aadAdminObjectId string

// --- SKUs (overridden per environment via .bicepparam) ---
@description('App Service Plan SKU.')
param appServicePlanSku string = 'B1'

@description('SQL database SKU name.')
param sqlDatabaseSkuName string = 'Basic'

@description('SQL database SKU tier.')
param sqlDatabaseSkuTier string = 'Basic'

@description('Service Bus namespace SKU.')
param serviceBusSku string = 'Standard'

@description('Entra (Azure AD) app registration client id for API auth. Public identifier, not a secret. Leave empty until the app registration exists.')
param entraClientId string = ''

@description('Deploy the VNet + private endpoints for SQL and Key Vault, and integrate the App Service/Worker into the VNet.')
param enablePrivateNetworking bool = true

@description('Disable public network access on SQL and Key Vault. Leave false for the first deploy (so migrations/seed can reach SQL); flip to true once the private endpoint is validated.')
param disablePublicNetworkAccess bool = false

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
    aadAdminLogin: aadAdminLogin
    aadAdminObjectId: aadAdminObjectId
    databaseSkuName: sqlDatabaseSkuName
    databaseSkuTier: sqlDatabaseSkuTier
    publicNetworkAccess: disablePublicNetworkAccess ? 'Disabled' : 'Enabled'
    tags: tags
  }
}

module keyVault 'modules/keyvault.bicep' = {
  name: 'keyVault'
  params: {
    keyVaultName: 'kv-${environmentName}-${suffix}'
    location: location
    publicNetworkAccess: disablePublicNetworkAccess ? 'Disabled' : 'Enabled'
    tags: tags
  }
}

// Private networking for the data tier (SQL + Key Vault). Consumes the SQL/KV resource ids,
// so it runs after those modules. Service Bus is excluded by design (needs Premium tier).
module network 'modules/network.bicep' = if (enablePrivateNetworking) {
  name: 'network'
  params: {
    namePrefix: namePrefix
    location: location
    sqlServerId: sql.outputs.serverId
    keyVaultId: keyVault.outputs.vaultId
    tags: tags
  }
}

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    workspaceName: 'log-${namePrefix}-${suffix}'
    appInsightsName: 'appi-${namePrefix}-${suffix}'
    location: location
    tags: tags
  }
}

// The Angular frontend is served as static files from the API App Service's wwwroot.
// No separate hosting resource is needed; UseStaticFiles + MapFallbackToFile handle routing.
module appService 'modules/appservice.bicep' = {
  name: 'appService'
  params: {
    planName: 'plan-${namePrefix}'
    webAppName: 'app-${namePrefix}-${suffix}'
    location: location
    skuName: appServicePlanSku
    sqlConnectionString: sql.outputs.connectionString
    serviceBusFullyQualifiedNamespace: serviceBus.outputs.fullyQualifiedNamespace
    keyVaultSecretUri: '${keyVault.outputs.vaultUri}secrets/ExternalApiKey/'
    // 'common' allows tokens from any Entra tenant and personal Microsoft accounts.
    // tenant().tenantId would lock the API to the subscription's own tenant only.
    entraTenantId: 'common'
    entraClientId: entraClientId
    appInsightsConnectionString: monitoring.outputs.connectionString
    aspNetCoreEnvironment: environmentName == 'prod' ? 'Production' : 'Development'
    // Guarded by the same condition as the network module, so the output is only read when it exists.
    #disable-next-line BCP318
    vnetIntegrationSubnetId: enablePrivateNetworking ? network.outputs.appSubnetId : ''
    tags: tags
  }
}

// Worker (Service Bus consumer) shares the API's App Service Plan.
module worker 'modules/appservice-worker.bicep' = {
  name: 'worker'
  params: {
    webAppName: 'wrk-${namePrefix}-${suffix}'
    location: location
    planId: appService.outputs.planId
    sqlConnectionString: sql.outputs.connectionString
    serviceBusFullyQualifiedNamespace: serviceBus.outputs.fullyQualifiedNamespace
    appInsightsConnectionString: monitoring.outputs.connectionString
    aspNetCoreEnvironment: environmentName == 'prod' ? 'Production' : 'Development'
    // Guarded by the same condition as the network module, so the output is only read when it exists.
    #disable-next-line BCP318
    vnetIntegrationSubnetId: enablePrivateNetworking ? network.outputs.appSubnetId : ''
    tags: tags
  }
}

// Grant both managed identities their data-plane roles (Service Bus + Key Vault).
// Runs after the apps because it consumes their principalIds.
module rbac 'modules/rbac.bicep' = {
  name: 'rbac'
  params: {
    principalId: appService.outputs.principalId
    workerPrincipalId: worker.outputs.principalId
    serviceBusNamespaceName: serviceBus.outputs.namespaceName
    keyVaultName: keyVault.outputs.vaultName
  }
}

@description('Public URL of the deployed API.')
output apiUrl string = 'https://${appService.outputs.defaultHostName}'

@description('SQL server FQDN.')
output sqlServerFqdn string = sql.outputs.fullyQualifiedDomainName

@description('Service Bus namespace id.')
output serviceBusNamespaceId string = serviceBus.outputs.namespaceId

@description('Key Vault URI — set the ExternalApiKey secret here after deploy.')
output keyVaultUri string = keyVault.outputs.vaultUri

@description('Web app managed identity principal id.')
output webAppPrincipalId string = appService.outputs.principalId

@description('Worker app URL (health endpoint).')
output workerUrl string = 'https://${worker.outputs.defaultHostName}'

@description('Worker managed identity principal id — needs a SQL contained user too.')
output workerPrincipalId string = worker.outputs.principalId

@description('Application Insights resource id.')
output appInsightsId string = monitoring.outputs.appInsightsId

@description('Log Analytics workspace id — alert rules query this.')
output logAnalyticsWorkspaceId string = monitoring.outputs.workspaceId

@description('Public URL of the Angular frontend (served from the API App Service wwwroot).')
output webUrl string = 'https://${appService.outputs.defaultHostName}'
