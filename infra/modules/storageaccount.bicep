// Azure Storage Account with static website hosting for the QuickCart Angular SPA.
// Standard_LRS / StorageV2 is the minimum required to enable the $web container
// and the static website endpoint (https://<account>.z[XX].web.core.windows.net).

@description('Storage account name (3-24 chars, lowercase alphanumeric).')
param accountName string

@description('Azure region.')
param location string

@description('Resource tags.')
param tags object = {}

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: accountName
  location: location
  tags: union(tags, { 'azd-service-name': 'web' })
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    allowBlobPublicAccess: true
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    accessTier: 'Hot'
  }
}

// Required: expose the $web blob container so azd can upload files into it.
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource webContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: '$web'
  properties: {
    publicAccess: 'Blob'
  }
}

// The static website primary endpoint includes a trailing slash; strip it here so the
// value is safe to use directly as a CORS allowed-origin string.
var rawWebEndpoint = storageAccount.properties.primaryEndpoints.web
// length() is always > 0 for a valid endpoint URL; BCP329 is a false positive here.
#disable-next-line BCP329
var webUrl = substring(rawWebEndpoint, 0, length(rawWebEndpoint) - 1)

@description('Static website URL without trailing slash (use as CORS allowed origin).')
output webUrl string = webUrl

@description('Storage account resource id.')
output id string = storageAccount.id
