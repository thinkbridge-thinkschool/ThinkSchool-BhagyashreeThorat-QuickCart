// App Service — hosts QuickCart.Api (Linux, .NET 10).

@description('App Service Plan name.')
param planName string

@description('Web App name (globally unique — becomes <name>.azurewebsites.net).')
param webAppName string

@description('Azure region.')
param location string

@description('App Service Plan SKU, e.g. B1, P0v3, P1v3.')
param skuName string = 'B1'

@description('.NET runtime stack for the Linux Web App.')
param linuxFxVersion string = 'DOTNETCORE|10.0'

@description('Passwordless SQL connection string (Managed Identity). Contains no secret.')
param sqlConnectionString string

@description('Service Bus fully-qualified namespace, e.g. sb-...servicebus.windows.net. Not a secret.')
param serviceBusFullyQualifiedNamespace string

@description('Key Vault reference URI for the ExternalApiKey secret (versionless). Resolved at runtime via the app MI.')
param keyVaultSecretUri string

@description('Entra (Azure AD) tenant id for app auth. Public identifier, not a secret.')
param entraTenantId string

@description('Entra (Azure AD) app registration client id for app auth. Public identifier, not a secret.')
param entraClientId string = ''

@description('ASPNETCORE_ENVIRONMENT value (Development / Production).')
param aspNetCoreEnvironment string = 'Production'

@description('Application Insights connection string for OpenTelemetry export. Not a secret.')
param appInsightsConnectionString string = ''

@description('Regional VNet integration subnet id. When set, outbound traffic to SQL/Key Vault flows through the VNet so the private-endpoint DNS resolves. Empty = no integration.')
param vnetIntegrationSubnetId string = ''

@description('Resource tags applied to every resource.')
param tags object = {}

resource plan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: planName
  location: location
  tags: tags
  sku: {
    name: skuName
  }
  kind: 'linux'
  properties: {
    reserved: true // required for Linux plans
  }
}

resource webApp 'Microsoft.Web/sites@2024-04-01' = {
  name: webAppName
  location: location
  tags: union(tags, { 'azd-service-name': 'api' })
  identity: {
    type: 'SystemAssigned' // ready for managed-identity auth later
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    // Regional VNet integration so calls to SQL/Key Vault resolve their private endpoints.
    virtualNetworkSubnetId: empty(vnetIntegrationSubnetId) ? null : vnetIntegrationSubnetId
    siteConfig: {
      linuxFxVersion: linuxFxVersion
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      alwaysOn: true
      // Route all outbound traffic through the VNet when integrated (needed for private DNS).
      vnetRouteAllEnabled: !empty(vnetIntegrationSubnetId)
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: aspNetCoreEnvironment
        }
        // Service Bus over Managed Identity: just the namespace, no SAS key.
        {
          name: 'ServiceBus__FullyQualifiedNamespace'
          value: serviceBusFullyQualifiedNamespace
        }
        // Entra ID app auth — public identifiers only, never secrets.
        {
          name: 'AzureAd__TenantId'
          value: entraTenantId
        }
        {
          name: 'AzureAd__ClientId'
          value: entraClientId
        }
        // Key Vault reference: App Service resolves this via its MI at runtime.
        // The app setting stores only the pointer, never the secret value.
        {
          name: 'ExternalApiKey'
          value: '@Microsoft.KeyVault(SecretUri=${keyVaultSecretUri})'
        }
        // OpenTelemetry → Application Insights. The distro reads this connection string.
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
      ]
      connectionStrings: [
        {
          name: 'DefaultConnection'
          connectionString: sqlConnectionString
          type: 'SQLAzure'
        }
      ]
    }
  }
}

@description('App Service Plan resource id — shared with the Worker app.')
output planId string = plan.id

@description('Web App resource id.')
output webAppId string = webApp.id

@description('Public default hostname.')
output defaultHostName string = webApp.properties.defaultHostName

@description('System-assigned managed identity principal id.')
output principalId string = webApp.identity.principalId
