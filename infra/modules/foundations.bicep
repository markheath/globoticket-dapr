// Foundational resources shared by everything: identity, observability,
// registry, secret store. All resource names use a stable token so the
// same env keeps the same names across redeploys.

param location string
param tags object
param resourceToken string

@description('Object ID of the deployer Entra principal. Granted Key Vault data access. Empty when running outside azd.')
param principalId string

// One shared user-assigned managed identity for all custom apps. Splitting
// per-app would be tighter for least-privilege but would multiply role
// assignments and complicate the demo. Documented as a hardening step.
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-${resourceToken}'
  location: location
  tags: tags
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-${resourceToken}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

// resourceToken is always 13 chars from uniqueString(), so the ACR name is always 16 chars.
// Bicep's flow analysis can't see that, hence the BCP334 length warning is harmless.
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: 'acr${resourceToken}'
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
  }
}

// ACR Pull on the MI lets apps pull images using the user-assigned identity.
var acrPullRoleId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'
resource acrPullForMi 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: containerRegistry
  name: guid(containerRegistry.id, managedIdentity.id, acrPullRoleId)
  properties: {
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRoleId)
  }
}

// Public images (mailpit, aspire-dashboard, caddy) are mirrored into this
// ACR by an azd postprovision hook (see scripts/import-public-images.ps1).
// Mirroring keeps the demo off Docker Hub's anonymous-pull rate limits;
// using a hook instead of a deploymentScript avoids the transient ACI
// provisioning hang that bit us on every re-provision.

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'kv-${resourceToken}'
  location: location
  tags: tags
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enablePurgeProtection: null
    publicNetworkAccess: 'Enabled'
  }
}

// Secrets User on the MI lets apps (and the Dapr secretstore component) read
// secrets from the vault.
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'
resource kvSecretsUserForMi 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, managedIdentity.id, keyVaultSecretsUserRoleId)
  properties: {
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
  }
}

// Secrets Officer on the deployer so the Bicep can write secret values
// into the vault during provisioning. Only assigned when principalId is
// supplied (i.e. when running through azd).
var keyVaultSecretsOfficerRoleId = 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7'
resource kvSecretsOfficerForDeployer 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(principalId)) {
  scope: keyVault
  name: guid(keyVault.id, principalId, keyVaultSecretsOfficerRoleId)
  properties: {
    principalId: principalId
    principalType: 'User'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsOfficerRoleId)
  }
}

output managedIdentityResourceId string = managedIdentity.id
output managedIdentityClientId string = managedIdentity.properties.clientId
output managedIdentityPrincipalId string = managedIdentity.properties.principalId
output managedIdentityName string = managedIdentity.name

output logAnalyticsWorkspaceId string = logAnalytics.id
output logAnalyticsCustomerId string = logAnalytics.properties.customerId

output containerRegistryEndpoint string = containerRegistry.properties.loginServer
output containerRegistryName string = containerRegistry.name

output keyVaultName string = keyVault.name
output keyVaultEndpoint string = keyVault.properties.vaultUri
