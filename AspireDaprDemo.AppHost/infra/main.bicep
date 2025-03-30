targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention, the name of the resource group for your application will use this name, prefixed with rg-')
param environmentName string

@minLength(1)
@description('The location used for all deployed resources')
param location string

@description('Id of the user or app to assign application roles')
param principalId string = ''


var tags = {
  'azd-env-name': environmentName
}

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}
module resources 'resources.bicep' = {
  scope: rg
  name: 'resources'
  params: {
    location: location
    tags: tags
    principalId: principalId
  }
}

module c_aspiredaprdemo 'c-aspiredaprdemo/c-aspiredaprdemo.module.bicep' = {
  name: 'c-aspiredaprdemo'
  scope: rg
  params: {
    location: location
    principalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
  }
}
module dapr 'dapr/dapr.bicep' = {
  name: 'dapr'
  scope: rg
  params: {
    clientId: ''
    cosmosCollectionName: 'cdc-aspiredaprdemo'
    cosmosDatabaseName: 'cdb-aspiredaprdemo'
    cosmosUrl: c_aspiredaprdemo.outputs.connectionString
    keyVaultName: 'kv-aspiredaprdemo'
    managedEnvironmentName: 'Development'
    messagingNamespace: 'sb-aspiredaprdemo.servicebus.windows.net'
  }
}
module kv_aspiredaprdemo 'kv-aspiredaprdemo/kv-aspiredaprdemo.module.bicep' = {
  name: 'kv-aspiredaprdemo'
  scope: rg
  params: {
    location: location
    principalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    principalType: 'ServicePrincipal'
  }
}
module sb_aspiredaprdemo 'sb-aspiredaprdemo/sb-aspiredaprdemo.module.bicep' = {
  name: 'sb-aspiredaprdemo'
  scope: rg
  params: {
    location: location
    principalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    principalType: 'ServicePrincipal'
  }
}

output MANAGED_IDENTITY_CLIENT_ID string = resources.outputs.MANAGED_IDENTITY_CLIENT_ID
output MANAGED_IDENTITY_NAME string = resources.outputs.MANAGED_IDENTITY_NAME
output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = resources.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_NAME
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = resources.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = resources.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID
output AZURE_CONTAINER_REGISTRY_NAME string = resources.outputs.AZURE_CONTAINER_REGISTRY_NAME
output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_NAME
output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN
output C_ASPIREDAPRDEMO_CONNECTIONSTRING string = c_aspiredaprdemo.outputs.connectionString
output KV_ASPIREDAPRDEMO_VAULTURI string = kv_aspiredaprdemo.outputs.vaultUri
output SB_ASPIREDAPRDEMO_SERVICEBUSENDPOINT string = sb_aspiredaprdemo.outputs.serviceBusEndpoint
