using 'main.bicep'

// Prod — bigger, production-grade SKUs.
param environmentName = 'prod'
param location = 'southeastasia' // subscription policy allows: austriaeast, uaenorth, southeastasia, koreacentral, malaysiawest

param appServicePlanSku = 'P1v3'              // dedicated, scalable
param sqlDatabaseSkuName = 'GP_S_Gen5_2'      // General Purpose serverless, 2 vCores
param sqlDatabaseSkuTier = 'GeneralPurpose'
param serviceBusSku = 'Premium'               // isolated, zone-resilient

param aadAdminLogin = '202401100142@msteams.mitaoe.ac.in'
param aadAdminObjectId = 'a320b42d-0b41-48da-a175-bd37c5188a2e'

param sqlAdministratorLogin = 'quickcartadmin'
// Never store the prod password in source. Pass it at deploy time or pull from Key Vault:
//   az deployment group create ... -p infra/main.prod.bicepparam -p sqlAdministratorLoginPassword='<secret>'
param sqlAdministratorLoginPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD', '')
