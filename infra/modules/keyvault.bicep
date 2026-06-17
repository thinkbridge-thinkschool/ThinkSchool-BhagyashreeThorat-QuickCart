// Key Vault — holds any remaining config that must stay out of source/app settings.
// App settings reference secrets via "@Microsoft.KeyVault(SecretUri=...)"; App Service
// resolves them at runtime using its managed identity (RBAC: Key Vault Secrets User,
// granted in rbac.bicep). No secret value is ever stored in app settings.

@description('Key Vault name (<= 24 chars, globally unique).')
@maxLength(24)
param keyVaultName string

@description('Azure region.')
param location string

@description('Resource tags applied to every resource.')
param tags object = {}

resource kv 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: tenant().tenantId
    enableRbacAuthorization: true // RBAC, not access policies — modern, auditable
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    publicNetworkAccess: 'Enabled'
  }
}

@description('Key Vault name.')
output vaultName string = kv.name

@description('Key Vault URI, e.g. https://<name>.vault.azure.net/ (trailing slash included).')
output vaultUri string = kv.properties.vaultUri

@description('Key Vault resource id (RBAC scope).')
output vaultId string = kv.id
