// Service Bus — carries QuickCart domain events between bounded contexts
// (OrderCreatedEvent, PaymentSucceededEvent, ...).

@description('Service Bus namespace name (globally unique).')
param namespaceName string

@description('Azure region for the namespace.')
param location string

@description('Namespace SKU. Basic = queues only; Standard = topics; Premium = isolated.')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param skuName string = 'Standard'

@description('Queue that order events are published to.')
param queueName string = 'order-events'

@description('Resource tags applied to every resource.')
param tags object = {}

resource namespace 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: namespaceName
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuName
  }
  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource queue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  parent: namespace
  name: queueName
  properties: {
    maxDeliveryCount: 10
    lockDuration: 'PT1M'
    deadLetteringOnMessageExpiration: true
  }
}

// App-facing send/listen authorization rule (kept off the root RootManageSharedAccessKey).
resource authRule 'Microsoft.ServiceBus/namespaces/authorizationRules@2024-01-01' = {
  parent: namespace
  name: 'QuickCartApp'
  properties: {
    rights: [
      'Send'
      'Listen'
    ]
  }
}

@description('Namespace resource id.')
output namespaceId string = namespace.id

@description('Service Bus connection string for the app (Send + Listen).')
@secure()
output connectionString string = authRule.listKeys().primaryConnectionString
