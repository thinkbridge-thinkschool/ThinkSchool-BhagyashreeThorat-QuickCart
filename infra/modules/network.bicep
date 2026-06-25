// Private networking for the data tier.
// Creates a VNet with two subnets, private DNS zones, and private endpoints for Azure SQL
// and Key Vault, so the App Service / Worker reach them over the Microsoft backbone instead
// of the public internet.
//
// Service Bus is intentionally EXCLUDED: Service Bus Private Link requires the *Premium*
// tier, and the dev environment runs Standard (see infra/main.dev.bicepparam). Adding a
// Service Bus private endpoint would force a ~10x cost increase, so it stays on the public
// endpoint (still locked to Managed Identity + RBAC, no SAS keys). The prod params already
// use Premium, so prod can add a Service Bus PE later with no app changes.

@description('Base name prefix, e.g. quickcart-dev.')
param namePrefix string

@description('Azure region.')
param location string

@description('Resource id of the SQL server to expose privately.')
param sqlServerId string

@description('Resource id of the Key Vault to expose privately.')
param keyVaultId string

@description('Resource tags applied to every resource.')
param tags object = {}

var vnetName = 'vnet-${namePrefix}'
var appSubnetName = 'snet-app'
var peSubnetName = 'snet-pe'

resource vnet 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: vnetName
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [ '10.10.0.0/16' ]
    }
    subnets: [
      {
        // App Service regional VNet integration subnet — delegated to serverFarms.
        name: appSubnetName
        properties: {
          addressPrefix: '10.10.1.0/24'
          delegations: [
            {
              name: 'webapp'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
            }
          ]
        }
      }
      {
        // Private-endpoint NICs land here. Network policies disabled so PEs can attach.
        name: peSubnetName
        properties: {
          addressPrefix: '10.10.2.0/24'
          privateEndpointNetworkPolicies: 'Disabled'
        }
      }
    ]
  }
}

var appSubnetId = '${vnet.id}/subnets/${appSubnetName}'
var peSubnetId = '${vnet.id}/subnets/${peSubnetName}'

// --- Private DNS zones (resolve the *.privatelink names to the PE private IPs) ---
resource sqlDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink${environment().suffixes.sqlServerHostname}'
  location: 'global'
  tags: tags
}

resource kvDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.vaultcore.azure.net'
  location: 'global'
  tags: tags
}

resource sqlDnsLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: sqlDnsZone
  name: 'link-${vnetName}'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: { id: vnet.id }
  }
}

resource kvDnsLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: kvDnsZone
  name: 'link-${vnetName}'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: { id: vnet.id }
  }
}

// --- SQL private endpoint ---
resource sqlPrivateEndpoint 'Microsoft.Network/privateEndpoints@2024-05-01' = {
  name: 'pe-sql-${namePrefix}'
  location: location
  tags: tags
  properties: {
    subnet: { id: peSubnetId }
    privateLinkServiceConnections: [
      {
        name: 'sql'
        properties: {
          privateLinkServiceId: sqlServerId
          groupIds: [ 'sqlServer' ]
        }
      }
    ]
  }
}

resource sqlPrivateDnsGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2024-05-01' = {
  parent: sqlPrivateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'sql'
        properties: { privateDnsZoneId: sqlDnsZone.id }
      }
    ]
  }
}

// --- Key Vault private endpoint ---
resource kvPrivateEndpoint 'Microsoft.Network/privateEndpoints@2024-05-01' = {
  name: 'pe-kv-${namePrefix}'
  location: location
  tags: tags
  properties: {
    subnet: { id: peSubnetId }
    privateLinkServiceConnections: [
      {
        name: 'kv'
        properties: {
          privateLinkServiceId: keyVaultId
          groupIds: [ 'vault' ]
        }
      }
    ]
  }
}

resource kvPrivateDnsGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2024-05-01' = {
  parent: kvPrivateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'kv'
        properties: { privateDnsZoneId: kvDnsZone.id }
      }
    ]
  }
}

@description('App Service regional VNet integration subnet id.')
output appSubnetId string = appSubnetId

@description('VNet resource id.')
output vnetId string = vnet.id
