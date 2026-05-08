// Data services: Postgres Flexible Server backing all persistent state
// (catalog data, ordering data, and the Dapr state stores for basket and
// workflow). Redis used to back the basket + workflow state stores, but
// Azure Cache for Redis provisions in 15-25 minutes — moving them to
// Postgres deletes Redis from the topology and shaves that off `azd up`.
//
// Authenticates via password. Postgres MI auth is scoped as the next
// hardening pass and would need a post-deploy role-assignment step.

param location string
param tags object
param resourceToken string

@secure()
@description('Auto-generated administrator password for Postgres. Set as a deployment-time secret; never read back from Bicep state.')
param postgresAdminPassword string = newGuid()

resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2023-12-01-preview' = {
  name: 'pg-${resourceToken}'
  location: location
  tags: tags
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    version: '17'
    administratorLogin: 'pgadmin'
    administratorLoginPassword: postgresAdminPassword
    storage: {
      storageSizeGB: 32
      autoGrow: 'Disabled'
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
    network: {
      publicNetworkAccess: 'Enabled'
    }
    authConfig: {
      activeDirectoryAuth: 'Disabled'
      passwordAuth: 'Enabled'
    }
  }

  resource catalogDb 'databases' = {
    name: 'catalogdb'
    properties: {
      charset: 'UTF8'
      collation: 'en_US.utf8'
    }
  }

  resource orderingDb 'databases' = {
    name: 'orderingdb'
    properties: {
      charset: 'UTF8'
      collation: 'en_US.utf8'
    }
  }

  // Backs the shopstate (basket) and workflowstate (Dapr Workflow actor)
  // state stores. Different `tableName` metadata keeps them separated.
  resource daprStateDb 'databases' = {
    name: 'daprstate'
    properties: {
      charset: 'UTF8'
      collation: 'en_US.utf8'
    }
  }

  // Allow Azure-internal traffic so Container Apps can reach the server
  // without a private endpoint. Closing this is the natural next step.
  resource allowAzureServices 'firewallRules' = {
    name: 'AllowAllAzureServices'
    properties: {
      startIpAddress: '0.0.0.0'
      endIpAddress: '0.0.0.0'
    }
  }
}

output postgresFqdn string = postgres.properties.fullyQualifiedDomainName
output postgresAdminLogin string = 'pgadmin'
@secure()
output postgresPassword string = postgresAdminPassword
