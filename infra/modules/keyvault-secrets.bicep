// Pushes secrets into the Key Vault. The Dapr components and container
// apps read these via the user-assigned MI, so neither component YAML
// nor container env vars ever contain the actual values.

param keyVaultName string

param postgresFqdn string
param postgresAdminLogin string = 'pgadmin'
@secure()
param postgresPassword string
param serviceBusNamespace string

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

// Catalog uses Aspire's Npgsql integration: AddNpgsqlDbContext("catalogdb")
// reads ConnectionStrings__catalogdb directly. The full connection string
// is the simplest unit to swap on rotation.
resource catalogConnectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'catalog-connection-string'
  properties: {
    value: 'Host=${postgresFqdn};Port=5432;Database=catalogdb;Username=${postgresAdminLogin};Password=${postgresPassword};SSL Mode=Require;Trust Server Certificate=true'
    contentType: 'text/plain'
  }
}

// Dapr orderstore component reads this directly via secretRef.
resource orderingConnectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'ordering-connection-string'
  properties: {
    value: 'host=${postgresFqdn} port=5432 user=${postgresAdminLogin} password=${postgresPassword} dbname=orderingdb sslmode=require'
    contentType: 'text/plain'
  }
}

// Shared by the shopstate and workflowstate Dapr components, which keep
// their data separate via different `tableName` metadata.
resource daprStateConnectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'daprstate-connection-string'
  properties: {
    value: 'host=${postgresFqdn} port=5432 user=${postgresAdminLogin} password=${postgresPassword} dbname=daprstate sslmode=require'
    contentType: 'text/plain'
  }
}

resource postgresPasswordSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'postgres-password'
  properties: {
    value: postgresPassword
    contentType: 'text/plain'
  }
}

// Recorded for reference; consumed via MI auth in the pubsub component
// so the value here is informational only.
resource serviceBusNamespaceSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'service-bus-namespace'
  properties: {
    value: serviceBusNamespace
    contentType: 'text/plain'
  }
}
