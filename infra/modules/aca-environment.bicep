// ACA managed environment plus all the Dapr components.
//
// Component auth model:
//   pubsub        -> Service Bus, managed identity (azureClientId)
//   secretstore   -> Key Vault, managed identity (vaultName + azureClientId)
//   shopstate     -> Postgres, connection string from Key Vault via component secret ref
//   workflowstate -> Postgres, connection string from Key Vault via component secret ref
//   orderstore    -> Postgres, full connection string loaded from Key Vault via component secret ref
//   sendmail      -> SMTP, points at the internal MailPit container app
//   scheduled     -> cron binding, no backing service
//
// In every case where a secret is needed, the component declares a Key
// Vault reference and an `identity:` (the user-assigned MI). The Dapr
// sidecar resolves the value at component init time. No secret value
// ever lives in the Bicep state or in container env vars.

param location string
param tags object
param resourceToken string

param logAnalyticsWorkspaceCustomerId string
param logAnalyticsWorkspaceId string

param keyVaultName string
param managedIdentityClientId string

param serviceBusNamespace string

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: last(split(logAnalyticsWorkspaceId, '/'))
}

resource acaEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'cae-${resourceToken}'
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspaceCustomerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
    daprAIInstrumentationKey: null
  }
}

// -------- pubsub: Service Bus topics, MI auth --------
resource pubsubComponent 'Microsoft.App/managedEnvironments/daprComponents@2024-03-01' = {
  parent: acaEnv
  name: 'pubsub'
  properties: {
    componentType: 'pubsub.azure.servicebus.topics'
    version: 'v1'
    metadata: [
      {
        name: 'namespaceName'
        value: serviceBusNamespace
      }
      {
        name: 'azureClientId'
        value: managedIdentityClientId
      }
      // Auto-create topic and subscriptions on first publish/subscribe so we
      // don't have to pre-declare topology in Bicep.
      {
        name: 'disableEntityManagement'
        value: 'false'
      }
    ]
    scopes: [
      'frontend'
      'ordering'
    ]
  }
}

// -------- secretstore: Azure Key Vault, MI auth --------
resource secretstoreComponent 'Microsoft.App/managedEnvironments/daprComponents@2024-03-01' = {
  parent: acaEnv
  name: 'secretstore'
  properties: {
    componentType: 'secretstores.azure.keyvault'
    version: 'v1'
    metadata: [
      {
        name: 'vaultName'
        value: keyVaultName
      }
      {
        name: 'azureClientId'
        value: managedIdentityClientId
      }
    ]
    scopes: [
      'ordering'
      'catalog'
      'frontend'
    ]
  }
}

// -------- shopstate: Postgres, connection string from Key Vault --------
// ACA's daprComponents schema does NOT support keyVaultUrl on per-component
// secrets. Instead, components reference a Dapr secretstore component by
// name; the Dapr runtime then resolves `secretRef` values through that
// component (which talks to Key Vault using the user-assigned MI).
//
// shopstate and workflowstate share the `daprstate` Postgres database with
// different `tableName` metadata so their data stays separated.
resource shopstateComponent 'Microsoft.App/managedEnvironments/daprComponents@2024-03-01' = {
  parent: acaEnv
  name: 'shopstate'
  properties: {
    componentType: 'state.postgresql'
    version: 'v2'
    secretStoreComponent: 'secretstore'
    metadata: [
      {
        name: 'connectionString'
        secretRef: 'daprstate-connection-string'
      }
      {
        name: 'tableName'
        value: 'basket_state'
      }
    ]
    scopes: [
      'frontend'
    ]
  }
  dependsOn: [
    secretstoreComponent
  ]
}

// -------- workflowstate: Postgres with actorStateStore: true --------
// Dapr Workflow rides on the actor runtime, so this store has to advertise
// itself as actor-capable. Postgres is on Dapr's actor-capable list (Redis
// is too — we picked Postgres to drop Azure Cache for Redis).
resource workflowstateComponent 'Microsoft.App/managedEnvironments/daprComponents@2024-03-01' = {
  parent: acaEnv
  name: 'workflowstate'
  properties: {
    componentType: 'state.postgresql'
    version: 'v2'
    secretStoreComponent: 'secretstore'
    metadata: [
      {
        name: 'connectionString'
        secretRef: 'daprstate-connection-string'
      }
      {
        name: 'tableName'
        value: 'workflow_state'
      }
      {
        name: 'actorStateStore'
        value: 'true'
      }
    ]
    scopes: [
      'ordering'
    ]
  }
  dependsOn: [
    secretstoreComponent
  ]
}

// -------- orderstore: Postgres, full connection string resolved via secretstore --------
resource orderstoreComponent 'Microsoft.App/managedEnvironments/daprComponents@2024-03-01' = {
  parent: acaEnv
  name: 'orderstore'
  properties: {
    componentType: 'state.postgresql'
    version: 'v2'
    secretStoreComponent: 'secretstore'
    metadata: [
      {
        name: 'connectionString'
        secretRef: 'ordering-connection-string'
      }
    ]
    scopes: [
      'ordering'
    ]
  }
  dependsOn: [
    secretstoreComponent
  ]
}

// -------- scheduled: cron, no backing service --------
resource scheduledComponent 'Microsoft.App/managedEnvironments/daprComponents@2024-03-01' = {
  parent: acaEnv
  name: 'scheduled'
  properties: {
    componentType: 'bindings.cron'
    version: 'v1'
    metadata: [
      {
        name: 'schedule'
        value: '@every 5m'
      }
    ]
    scopes: [
      'catalog'
    ]
  }
}

// -------- sendmail: SMTP via the internal MailPit container app --------
// MailPit is reached by short app name. The `<app>.internal.<envdomain>`
// FQDN resolves to the env's HTTP ingress proxy, which has no raw-TCP
// listener in Consumption-only (no-VNet) envs — SMTP dials to it
// black-hole and time out. The short name resolves to the app's
// cluster-internal service IP and accepts raw TCP.
//
// No user/password: the Dapr SMTP binding uses Go's smtp.PlainAuth, which
// refuses to transmit credentials over a non-TLS connection ("unencrypted
// connection" error). MailPit runs with MP_SMTP_AUTH_ACCEPT_ANY and is
// happy to accept unauthenticated mail, so we omit AUTH entirely.
resource sendmailComponent 'Microsoft.App/managedEnvironments/daprComponents@2024-03-01' = {
  parent: acaEnv
  name: 'sendmail'
  properties: {
    componentType: 'bindings.smtp'
    version: 'v1'
    metadata: [
      {
        name: 'host'
        value: 'mailpit'
      }
      {
        name: 'port'
        value: '1025'
      }
      {
        name: 'skipTLSVerify'
        value: 'true'
      }
    ]
    scopes: [
      'ordering'
    ]
  }
}

output environmentId string = acaEnv.id
output environmentName string = acaEnv.name
output environmentDefaultDomain string = acaEnv.properties.defaultDomain
output environmentStaticIp string = acaEnv.properties.staticIp
