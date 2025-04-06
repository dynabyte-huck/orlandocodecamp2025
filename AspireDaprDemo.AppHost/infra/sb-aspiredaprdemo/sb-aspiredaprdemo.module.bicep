@description('The location for the resource(s) to be deployed.')
param location string = ''

param sku string = 'Standard'

param principalType string

param principalId string

resource sb_aspiredaprdemo 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: take('sbaspiredaprdemo-${uniqueString(resourceGroup().id)}', 50)
  location: location
  properties: {
    disableLocalAuth: true
  }
  sku: {
    name: sku
  }
  tags: {
    'aspire-resource-name': 'sb-aspiredaprdemo'
  }
}

resource sb_aspiredaprdemo_AzureServiceBusDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(sb_aspiredaprdemo.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
    principalType: principalType
  }
  scope: sb_aspiredaprdemo
}

resource sbq_aspiredaprdemo 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  name: 'sbq-aspiredaprdemo'
  parent: sb_aspiredaprdemo
}

output serviceBusEndpoint string = sb_aspiredaprdemo.properties.serviceBusEndpoint