// Subscription-scoped entry point. Creates a single resource group and
// hands off to a module that provisions everything inside it. `azd down`
// can tear the whole thing down by deleting just the resource group.
targetScope = 'subscription'

@minLength(1)
@maxLength(48)
@description('Short environment name. Used as a prefix and in azd-env-name tag.')
param environmentName string

@minLength(1)
@description('Azure region for all resources. Default uksouth — closest to UK with broad SKU coverage.')
param location string = 'uksouth'

@description('Object ID of the Entra principal running the deployment. Granted Key Vault data access for inspection. Supplied automatically by azd.')
param principalId string = ''

var tags = {
  'azd-env-name': environmentName
}

var resourceGroupName = 'rg-${environmentName}'

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

module resources 'modules/resources.bicep' = {
  name: 'resources'
  scope: rg
  params: {
    environmentName: environmentName
    location: location
    principalId: principalId
    tags: tags
  }
}

output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
output AZURE_RESOURCE_GROUP string = rg.name
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = resources.outputs.containerRegistryEndpoint
output AZURE_CONTAINER_REGISTRY_NAME string = resources.outputs.containerRegistryName
output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = resources.outputs.containerAppsEnvironmentId
output AZURE_KEY_VAULT_ENDPOINT string = resources.outputs.keyVaultEndpoint
output AZURE_KEY_VAULT_NAME string = resources.outputs.keyVaultName
output FRONTEND_URL string = resources.outputs.frontendUrl
output ASPIRE_DASHBOARD_URL string = resources.outputs.dashboardUrl
