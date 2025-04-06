@description('The location for the resource(s) to be deployed.')
param location string = ''

param principalType string

param principalId string

resource kv_aspiredaprdemo 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: take('kvaspiredaprdemo-${uniqueString(resourceGroup().id)}', 24)
  location: location
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
  }
  tags: {
    'aspire-resource-name': 'kv-aspiredaprdemo'
  }
}

resource kv_aspiredaprdemo_KeyVaultAdministrator 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(kv_aspiredaprdemo.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483')
    principalType: principalType
  }
  scope: kv_aspiredaprdemo
}

output vaultUri string = kv_aspiredaprdemo.properties.vaultUri