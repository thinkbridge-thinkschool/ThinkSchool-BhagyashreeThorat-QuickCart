// Azure SQL — real persistence to replace the EF Core InMemory provider
// (DESIGN.md: "swappable for SQL Server in AddInfrastructure").

@description('Logical SQL Server name (globally unique).')
param serverName string

@description('SQL database name.')
param databaseName string

@description('Azure region.')
param location string

@description('SQL administrator login.')
param administratorLogin string

@description('SQL administrator password.')
@secure()
param administratorLoginPassword string

@description('Database SKU name, e.g. Basic, S0, GP_S_Gen5_1.')
param databaseSkuName string = 'Basic'

@description('Database SKU tier, e.g. Basic, Standard, GeneralPurpose.')
param databaseSkuTier string = 'Basic'

@description('Max database size in bytes.')
param maxSizeBytes int = 2147483648 // 2 GB

@description('Resource tags applied to every resource.')
param tags object = {}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: serverName
  location: location
  tags: tags
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorLoginPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource database 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  tags: tags
  sku: {
    name: databaseSkuName
    tier: databaseSkuTier
  }
  properties: {
    maxSizeBytes: maxSizeBytes
    zoneRedundant: false
  }
}

// Allow other Azure services (e.g. the App Service) to reach the server.
// The 0.0.0.0 sentinel rule is Azure's documented "Allow Azure services" toggle.
resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAllAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

@description('SQL Server resource id.')
output serverId string = sqlServer.id

@description('Fully qualified server name, e.g. myserver.database.windows.net.')
output fullyQualifiedDomainName string = sqlServer.properties.fullyQualifiedDomainName

@description('ADO.NET connection string for the app.')
@secure()
output connectionString string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${databaseName};Persist Security Info=False;User ID=${administratorLogin};Password=${administratorLoginPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
