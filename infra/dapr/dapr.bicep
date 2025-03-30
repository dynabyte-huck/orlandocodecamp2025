param managedEnvironmentName string
param keyVaultName string
param clientId string
param cosmosUrl string
param messagingNamespace string
param cosmosCollectionName string
param cosmosDatabaseName string

resource managedEnvironment 'Microsoft.App/managedEnvironments@2024-10-02-preview' existing = {
  name: managedEnvironmentName 
}

resource secrets 'Microsoft.App/managedEnvironments/daprComponents@2024-10-02-preview' = {
  parent: managedEnvironment
  name: 'demosecretstore'
  properties: {
    componentType: 'secretstores.azure.keyvault'
    metadata: [
      {
        name: 'vaultName'
        value: keyVaultName
      }
      {
        name: 'azureClientId'
        value: clientId
      }
    ]
    version: 'v1'
  }
}

resource pubsub 'Microsoft.App/managedEnvironments/daprComponents@2024-10-02-preview' = {
  parent: managedEnvironment
  name: 'demopubsub'
  properties: {
    componentType: 'pubsub.azure.servicebus.queues'
    metadata: [
      {
        name: 'namespaceName'
        value: messagingNamespace
      }
      {
        name: 'azureClientId'
        value: clientId
      }
    ]
    version: 'v1'
  }
}

resource statestore 'Microsoft.App/managedEnvironments/daprComponents@2024-10-02-preview' = {
  parent: managedEnvironment
  name: 'demostore'
  properties: {
    componentType: 'state.azure.cosmosdb'
    ignoreErrors: false
    metadata: [
      {
        name: 'url'
        value: cosmosUrl
      }
      {
        name: 'database'
        value: cosmosDatabaseName
      }
      {
        name: 'collection'
        value: cosmosCollectionName
      }
      {
        name: 'azureClientId'
        value: clientId
      }
    ]
    version: 'v1'
  }
}

resource demoCronJob 'Microsoft.App/managedEnvironments/daprComponents@2024-10-02-preview' = {
  parent: managedEnvironment
  name: 'democronjob'
  properties: {
    componentType: 'bindings.cron'
    version: 'v1'
    metadata: [
      {
        name: 'schedule'
        value: '@every 60m'
      }
      {
        name: 'direction'
        value: 'input'
      }
    ]
    scopes: [
      'demowrkflwsvc'
    ]
  }
}
