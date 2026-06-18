// App Service (Worker) — hosts QuickCart.Worker, the Service Bus consumer.
// Runs as a Linux web app on the SAME plan as the API (passed in via planId) to save cost.
// It binds a port for health probes; the real work happens in its hosted BackgroundService.

@description('Web App name (globally unique — becomes <name>.azurewebsites.net).')
param webAppName string

@description('Azure region.')
param location string

@description('Resource id of the App Service Plan shared with the API.')
param planId string

@description('.NET runtime stack for the Linux Web App.')
param linuxFxVersion string = 'DOTNETCORE|10.0'

@description('Passwordless SQL connection string (Managed Identity). Contains no secret.')
param sqlConnectionString string

@description('Service Bus fully-qualified namespace. Not a secret.')
param serviceBusFullyQualifiedNamespace string

@description('Application Insights connection string for OpenTelemetry export. Not a secret.')
param appInsightsConnectionString string

@description('ASPNETCORE_ENVIRONMENT value (Development / Production).')
param aspNetCoreEnvironment string = 'Production'

@description('Resource tags applied to every resource.')
param tags object = {}

resource webApp 'Microsoft.Web/sites@2024-04-01' = {
  name: webAppName
  location: location
  tags: union(tags, { 'azd-service-name': 'worker' })
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: planId
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: linuxFxVersion
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      alwaysOn: true // keep the consumer running; without this App Service idles the process out
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: aspNetCoreEnvironment
        }
        {
          name: 'ServiceBus__FullyQualifiedNamespace'
          value: serviceBusFullyQualifiedNamespace
        }
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

@description('Worker web app default hostname.')
output defaultHostName string = webApp.properties.defaultHostName

@description('Worker system-assigned managed identity principal id.')
output principalId string = webApp.identity.principalId
