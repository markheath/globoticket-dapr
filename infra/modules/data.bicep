// Data services: Postgres Flexible Server (catalog + ordering DBs) and
// Azure Cache for Redis (basket + workflow state).
//
// Both currently authenticate via password/key. Postgres MI auth and
// Redis Entra auth are scoped as the next hardening pass — both require
// either post-deploy steps (Postgres) or higher SKUs (Redis Entra).

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

resource redis 'Microsoft.Cache/Redis@2024-03-01' = {
  name: 'redis-${resourceToken}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'Standard'
      family: 'C'
      capacity: 0
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    redisConfiguration: {
      'maxmemory-policy': 'allkeys-lru'
    }
  }
}

output postgresFqdn string = postgres.properties.fullyQualifiedDomainName
output postgresAdminLogin string = 'pgadmin'
@secure()
output postgresPassword string = postgresAdminPassword

output redisHostname string = redis.properties.hostName
@secure()
output redisPassword string = redis.listKeys().primaryKey
