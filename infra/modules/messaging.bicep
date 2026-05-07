// Service Bus namespace used by the Dapr pubsub component. Authenticated
// from the apps via managed identity — no connection strings anywhere.

param location string
param tags object
param resourceToken string

@description('Principal ID of the user-assigned MI used by the apps. Granted Service Bus Data Sender + Receiver.')
param managedIdentityPrincipalId string

resource serviceBus 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: 'sb-${resourceToken}'
  location: location
  tags: tags
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
  }
}

// Service Bus Data Owner — gives the MI the rights it needs to publish AND
// subscribe AND let Dapr auto-create the topic + subscriptions on first
// use. Splitting into Sender + Receiver requires pre-creating the topology,
// which would mean either Bicep declaring the topic/subscription shape or
// a post-deploy script. Owner is acceptable for the demo; tightening to
// Sender + Receiver after pre-creating topology is the next pass.
var serviceBusDataOwnerRoleId = '090c5cfd-751d-490a-894a-3ce6f1109419'
resource sbDataOwnerForMi 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: serviceBus
  name: guid(serviceBus.id, managedIdentityPrincipalId, serviceBusDataOwnerRoleId)
  properties: {
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', serviceBusDataOwnerRoleId)
  }
}

output serviceBusNamespace string = '${serviceBus.name}.servicebus.windows.net'
output serviceBusName string = serviceBus.name
