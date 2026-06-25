// Monitoring — Log Analytics workspace + workspace-based Application Insights.
// Both the API and the Worker export OpenTelemetry to this App Insights resource;
// the queries (requests / dependencies) and the distributed trace live in this workspace.

@description('Log Analytics workspace name.')
param workspaceName string

@description('Application Insights component name.')
param appInsightsName string

@description('Azure region.')
param location string

@description('Log retention in days.')
param retentionInDays int = 30

@description('Resource tags applied to every resource.')
param tags object = {}

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: workspaceName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: retentionInDays
  }
}

// Workspace-based App Insights: telemetry lands in the Log Analytics workspace above,
// so KQL runs against the same store (requests, dependencies, traces, exceptions tables).
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: workspace.id
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

@description('Log Analytics workspace resource id (alert rules and diagnostics target this).')
output workspaceId string = workspace.id

@description('Application Insights resource id.')
output appInsightsId string = appInsights.id

@description('Application Insights connection string — set as APPLICATIONINSIGHTS_CONNECTION_STRING on each service.')
output connectionString string = appInsights.properties.ConnectionString
