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

// No SAS authorization rule and no listKeys() — the app authenticates with its
// managed identity (RBAC: Azure Service Bus Data Sender/Receiver, granted in rbac.bicep).
// Nothing here ever emits a connection string, so there is no secret to leak.

@description('Namespace resource id.')
output namespaceId string = namespace.id

@description('Namespace name (used to build the fully-qualified namespace for DefaultAzureCredential).')
output namespaceName string = namespace.name

@description('Fully-qualified namespace, e.g. sb-quickcart-dev-xxxx.servicebus.windows.net.')
output fullyQualifiedNamespace string = '${namespace.name}.servicebus.windows.net'
