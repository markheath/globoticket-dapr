// ACA managed environment plus all the Dapr components.
//
// Component auth model:
//   pubsub        -> Service Bus, managed identity (azureClientId)
//   secretstore   -> Key Vault, managed identity (vaultName + azureClientId)
//   shopstate     -> Redis, password loaded from Key Vault via component secret ref
//   workflowstate -> Redis, password loaded from Key Vault via component secret ref
//   orderstore    -> Postgres, full connection string loaded from Key Vault via component secret ref
//   sendmail      -> SMTP, points at the internal MailPit container app, creds from Key Vault
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
param redisHostname string

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

// -------- shopstate: Redis, password resolved via the secretstore component --------
// ACA's daprComponents schema does NOT support keyVaultUrl on per-component
// secrets. Instead, components reference a Dapr secretstore component by
// name; the Dapr runtime then resolves `secretRef` values through that
// component (which talks to Key Vault using the user-assigned MI).
resource shopstateComponent 'Microsoft.App/managedEnvironments/daprComponents@2024-03-01' = {
  parent: acaEnv
  name: 'shopstate'
  properties: {
    componentType: 'state.redis'
    version: 'v1'
    secretStoreComponent: 'secretstore'
    metadata: [
      {
        name: 'redisHost'
        value: '${redisHostname}:6380'
      }
      {
        name: 'redisPassword'
        secretRef: 'redis-password'
      }
      {
        name: 'enableTLS'
        value: 'true'
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

// -------- workflowstate: Redis with actorStateStore: true --------
// Dapr Workflow rides on the actor runtime, so this store has to advertise
// itself as actor-capable.
resource workflowstateComponent 'Microsoft.App/managedEnvironments/daprComponents@2024-03-01' = {
  parent: acaEnv
  name: 'workflowstate'
  properties: {
    componentType: 'state.redis'
    version: 'v1'
    secretStoreComponent: 'secretstore'
    metadata: [
      {
        name: 'redisHost'
        value: '${redisHostname}:6380'
      }
      {
        name: 'redisPassword'
        secretRef: 'redis-password'
      }
      {
        name: 'enableTLS'
        value: 'true'
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
// Hostname is the env's internal DNS for the mailpit app. Creds are pulled
// via the secretstore component (Key Vault-backed) so the component spec
// has no plaintext credentials.
resource sendmailComponent 'Microsoft.App/managedEnvironments/daprComponents@2024-03-01' = {
  parent: acaEnv
  name: 'sendmail'
  properties: {
    componentType: 'bindings.smtp'
    version: 'v1'
    secretStoreComponent: 'secretstore'
    metadata: [
      {
        name: 'host'
        // ACA's internal DNS shortname works for Dapr service invocation
        // but not for plain TCP binding traffic like SMTP. Use the fully
        // qualified internal FQDN so the SMTP dial resolves correctly.
        value: 'mailpit.internal.${acaEnv.properties.defaultDomain}'
      }
      {
        name: 'port'
        value: '1025'
      }
      {
        name: 'user'
        secretRef: 'smtp-user'
      }
      {
        name: 'password'
        secretRef: 'smtp-password'
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
  dependsOn: [
    secretstoreComponent
  ]
}

output environmentId string = acaEnv.id
output environmentName string = acaEnv.name
output environmentDefaultDomain string = acaEnv.properties.defaultDomain
output environmentStaticIp string = acaEnv.properties.staticIp
