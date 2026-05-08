// Orchestrator at resource-group scope. All resource names are derived
// from a stable `resourceToken` so repeat deployments to the same env
// land on the same names.

@description('Short environment name.')
param environmentName string

@description('Region for all resources.')
param location string

@description('Deployer principal object ID. Empty when running outside azd.')
param principalId string

@description('Tags to apply to every resource.')
param tags object

var resourceToken = uniqueString(subscription().subscriptionId, resourceGroup().id, environmentName)

// -------- Foundations: identity, registry, secrets, observability --------

module foundations 'foundations.bicep' = {
  name: 'foundations'
  params: {
    location: location
    tags: tags
    resourceToken: resourceToken
    principalId: principalId
  }
}

// -------- Data + messaging backing services --------

module data 'data.bicep' = {
  name: 'data'
  params: {
    location: location
    tags: tags
    resourceToken: resourceToken
  }
}

module messaging 'messaging.bicep' = {
  name: 'messaging'
  params: {
    location: location
    tags: tags
    resourceToken: resourceToken
    managedIdentityPrincipalId: foundations.outputs.managedIdentityPrincipalId
  }
}

// -------- Push secrets into Key Vault --------
// The user-assigned MI is granted Secrets User against the vault, then
// the components reference these secret names.

module secrets 'keyvault-secrets.bicep' = {
  name: 'kv-secrets'
  params: {
    keyVaultName: foundations.outputs.keyVaultName
    postgresFqdn: data.outputs.postgresFqdn
    postgresAdminLogin: data.outputs.postgresAdminLogin
    postgresPassword: data.outputs.postgresPassword
    serviceBusNamespace: messaging.outputs.serviceBusNamespace
  }
}

// -------- ACA environment + Dapr components --------

module acaEnv 'aca-environment.bicep' = {
  name: 'aca-env'
  params: {
    location: location
    tags: tags
    resourceToken: resourceToken
    logAnalyticsWorkspaceCustomerId: foundations.outputs.logAnalyticsCustomerId
    logAnalyticsWorkspaceId: foundations.outputs.logAnalyticsWorkspaceId
    keyVaultName: foundations.outputs.keyVaultName
    managedIdentityClientId: foundations.outputs.managedIdentityClientId
    serviceBusNamespace: messaging.outputs.serviceBusNamespace
  }
  dependsOn: [
    secrets
  ]
}

// -------- ACA apps --------

module apps 'apps.bicep' = {
  name: 'apps'
  params: {
    location: location
    tags: tags
    containerAppsEnvironmentId: acaEnv.outputs.environmentId
    containerRegistryLoginServer: foundations.outputs.containerRegistryEndpoint
    managedIdentityResourceId: foundations.outputs.managedIdentityResourceId
    managedIdentityClientId: foundations.outputs.managedIdentityClientId
    keyVaultName: foundations.outputs.keyVaultName
  }
}

output containerRegistryEndpoint string = foundations.outputs.containerRegistryEndpoint
output containerRegistryName string = foundations.outputs.containerRegistryName
output containerAppsEnvironmentId string = acaEnv.outputs.environmentId
output keyVaultEndpoint string = foundations.outputs.keyVaultEndpoint
output keyVaultName string = foundations.outputs.keyVaultName
output frontendUrl string = apps.outputs.frontendUrl
output dashboardUrl string = apps.outputs.dashboardUrl
output mailpitUiUrl string = apps.outputs.mailpitUiUrl
