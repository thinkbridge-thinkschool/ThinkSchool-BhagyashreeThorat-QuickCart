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

@description('SQL connection string injected as ConnectionStrings__DefaultConnection.')
@secure()
param sqlConnectionString string

@description('Service Bus connection string injected as app setting.')
@secure()
param serviceBusConnectionString string

@description('ASPNETCORE_ENVIRONMENT value (Development / Production).')
param aspNetCoreEnvironment string = 'Production'

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
  tags: tags
  identity: {
    type: 'SystemAssigned' // ready for managed-identity auth later
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: linuxFxVersion
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      alwaysOn: true
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: aspNetCoreEnvironment
        }
        {
          name: 'ServiceBus__ConnectionString'
          value: serviceBusConnectionString
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

@description('Web App resource id.')
output webAppId string = webApp.id

@description('Public default hostname.')
output defaultHostName string = webApp.properties.defaultHostName

@description('System-assigned managed identity principal id.')
output principalId string = webApp.identity.principalId
