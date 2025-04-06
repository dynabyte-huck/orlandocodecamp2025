@description('The location for the resource(s) to be deployed.')
param location string = ''

param principalId string

resource c_aspiredaprdemo 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' = {
  name: take('caspiredaprdemo-${uniqueString(resourceGroup().id)}', 44)
  location: location
  properties: {
    locations: [
      {
        locationName: location
        failoverPriority: 0
      }
    ]
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    databaseAccountOfferType: 'Standard'
    disableLocalAuth: true
  }
  kind: 'GlobalDocumentDB'
  tags: {
    'aspire-resource-name': 'c-aspiredaprdemo'
  }
}

resource cdb_aspiredaprdemo 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-08-15' = {
  name: 'cdb-aspiredaprdemo'
  location: location
  properties: {
    resource: {
      id: 'cdb-aspiredaprdemo'
    }
  }
  parent: c_aspiredaprdemo
}

resource cdc_aspiredaprdemo 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
  name: 'cdc-aspiredaprdemo'
  location: location
  properties: {
    resource: {
      id: 'cdc-aspiredaprdemo'
      partitionKey: {
        paths: [
          '/id'
        ]
      }
    }
  }
  parent: cdb_aspiredaprdemo
}

resource c_aspiredaprdemo_roleDefinition 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2024-08-15' existing = {
  name: '00000000-0000-0000-0000-000000000002'
  parent: c_aspiredaprdemo
}

resource c_aspiredaprdemo_roleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-08-15' = {
  name: guid(principalId, c_aspiredaprdemo_roleDefinition.id, c_aspiredaprdemo.id)
  properties: {
    principalId: principalId
    roleDefinitionId: c_aspiredaprdemo_roleDefinition.id
    scope: c_aspiredaprdemo.id
  }
  parent: c_aspiredaprdemo
}

output connectionString string = c_aspiredaprdemo.properties.documentEndpoint