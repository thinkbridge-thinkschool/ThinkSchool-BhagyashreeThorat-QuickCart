using 'main.bicep'

// Dev — cheapest SKUs that still deploy.
param environmentName = 'dev'
param location = 'southeastasia' // subscription policy allows: austriaeast, uaenorth, southeastasia, koreacentral, malaysiawest

param appServicePlanSku = 'B1'
param sqlDatabaseSkuName = 'Basic'
param sqlDatabaseSkuTier = 'Basic'
param serviceBusSku = 'Standard'

param sqlAdministratorLogin = 'quickcartadmin'
// Do NOT hardcode the real password. Supply it at deploy time:
//   az deployment group create ... -p infra/main.dev.bicepparam -p sqlAdministratorLoginPassword='<secret>'
// or read from Key Vault. The line below is a placeholder for what-if only.
param sqlAdministratorLoginPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD', 'ChangeMe-Dev-1234!')
