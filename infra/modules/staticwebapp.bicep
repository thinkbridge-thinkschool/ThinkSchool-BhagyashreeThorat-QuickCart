// Azure Static Web App — hosts the QuickCart Angular frontend.
// Free tier is sufficient for a demo/dev deployment.

@description('Static Web App resource name (globally unique).')
param appName string

@description('Azure region for the SWA management plane.')
param location string

@description('Resource tags applied to the SWA resource.')
param tags object = {}

resource swa 'Microsoft.Web/staticSites@2024-04-01' = {
  name: appName
  location: location
  tags: union(tags, { 'azd-service-name': 'web' })
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
    provider: 'Custom'
    enterpriseGradeCdnStatus: 'Disabled'
  }
}

@description('Default hostname of the deployed SWA, e.g. purple-meadow-123.azurestaticapps.net.')
output defaultHostName string = swa.properties.defaultHostname

@description('SWA resource id.')
output id string = swa.id
