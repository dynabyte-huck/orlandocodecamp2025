using Aspire.Hosting.Azure.CosmosDB;
using Aspire.Hosting.Azure.ServiceBus;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Microsoft.Extensions.Hosting;

using StackExchange.Redis;

// set up some constants
const string APP_ID = "APP_ID";
const string DAPR_LOCAL_CONFIG = "./dapr/config.yaml";

var builder = DistributedApplication.CreateBuilder(args);

// Configure Dapr.io
builder.AddDapr();

// create our apps and services
var notificationService = builder.AddProject<Projects.AspireDaprDemo_NotificationService>("aspiredaprdemo-notificationservice")
    .WithEnvironment(APP_ID, "demonotsvc");

var workflowService = builder.AddProject<Projects.AspireDaprDemo_WorkflowService>("aspiredaprdemo-workflowservice")
    .WithEnvironment(APP_ID, "demowrkflwsvc");

var subscriberService = builder.AddProject<Projects.AspireDaprDemo_SubscriberService>("aspiredaprdemo-subscriberservice")
    .WithEnvironment(APP_ID, "demosubsvc");

var weatherService = builder.AddProject<Projects.AspireDaprDemo_WeatherService>("aspiredaprdemo-weatherservice")
    .WithEnvironment(APP_ID, "demowthrsvc");

var frontend = builder.AddNpmApp("aspiredaprdemo-frontend", "../aspiredaprdemo_frontend", scriptName: "dev")
    .WithReference(subscriberService)
    .WithReference(weatherService)
    .WithEnvironment("VITE_WEATHER_URL", weatherService.GetEndpoint("https"))
    .WithEnvironment("VITE_SUBSCRIBER_URL", subscriberService.GetEndpoint("https"))
    .WaitFor(weatherService)
    .WaitFor(subscriberService);

notificationService.WithReference(frontend);
workflowService.WithReference(frontend);
subscriberService.WithReference(frontend);
weatherService.WithReference(frontend);

// configure for local development
if (builder.ExecutionContext.IsRunMode)
{
    // Add supporting technologies locally
    var redis = builder.AddRedis("redis");
    var username = builder.AddParameter("demostatestoreuser", secret: true);
    var password = builder.AddParameter("demostatestorepassword", secret: true);
    var postgres = builder.AddPostgres("postgres", username, password, 8501)
        .WithLifetime(ContainerLifetime.Persistent);
    var maildev = builder.AddContainer("maildev", "maildev/maildev")
        .WithLifetime(ContainerLifetime.Persistent)
        .WithHttpEndpoint(port: 1080, targetPort: 1080)
        .WithEndpoint(port: 1025, targetPort: 1025);

    // Configure Dapr.io components locally
    var secretStore = builder.AddDaprComponent("demosecretstore",
        "secretstores.local.file",
        new DaprComponentOptions
        {
            LocalPath = "./dapr/secretstore.yml"
        });
    var pubSub = builder.AddDaprPubSub("demopubsub",
        new DaprComponentOptions
        {
            LocalPath = "./dapr/pubsub.yml"
        }).WaitFor(redis);
    var stateStore = builder.AddDaprStateStore("demostore",
        new DaprComponentOptions
        {
            LocalPath = "./dapr/statestore.yml"
        }).WaitFor(postgres);
    var cronBinding = builder.AddDaprComponent("democronjobwip",
        "bindings.cron",
        new DaprComponentOptions
        {
            LocalPath = "./dapr/cron.yml"
        });

    // Configure services
    notificationService
        .WithDaprSidecar(new DaprSidecarOptions
        {
            AppId = "demonotsvc",
            AppProtocol = "https",
            Config = DAPR_LOCAL_CONFIG,
            AppPort = 7271,
            DaprHttpPort = 3501,
        })
        .WithReference(secretStore)
        .WithReference(stateStore)
        .WithReference(pubSub)
        .WaitFor(postgres)
        .WaitFor(maildev);

    workflowService
        .WithDaprSidecar(new DaprSidecarOptions
        {
            AppId = "demowrkflwsvc",
            AppProtocol = "https",
            Config = DAPR_LOCAL_CONFIG,
            AppPort = 7011,
            DaprHttpPort = 3502,
        })
        .WithReference(secretStore)
        .WithReference(stateStore)
        .WithReference(pubSub)
        .WithReference(cronBinding)
        .WaitFor(postgres)
        .WaitFor(maildev);

    subscriberService
        .WithDaprSidecar(new DaprSidecarOptions
        {
            AppId = "demosubsvc",
            AppProtocol = "https",
            Config = DAPR_LOCAL_CONFIG,
            AppPort = 7017,
            DaprHttpPort = 3503,
        })
        .WithReference(secretStore)
        .WithReference(stateStore)
        .WithReference(pubSub)
        .WaitFor(postgres)
        .WaitFor(maildev);

    weatherService
        .WithDaprSidecar(new DaprSidecarOptions
        {
            AppId = "demowthrsvc",
            AppProtocol = "https",
            Config = DAPR_LOCAL_CONFIG,
            AppPort = 7272,
            DaprHttpPort = 3504,
        })
        .WithReference(secretStore)
        .WithReference(stateStore)
        .WithReference(pubSub)
        .WaitFor(postgres)
        .WaitFor(maildev);

    frontend
        .WithHttpsEndpoint(port: 5141, targetPort: 5141, name: "frontend-https", isProxied: false);

    // Executable to Host Dapr Dashboard - because we can
    builder.AddExecutable("dapr-dashboard", "dapr", ".", "dashboard", "-p", "8085")
        .WithHttpEndpoint(port: 8085, targetPort: 8085, name: "dapr-dashboard-http", isProxied: false);
}

if (builder.ExecutionContext.IsPublishMode)
{
    Console.WriteLine("Publishing to Azure");
    builder.AddAzureProvisioning();
    string tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID")!;
    string clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID")!;
    const string SERVICE_BUS_NAME = "sb-aspiredaprdemo";
    const string QUEUE_NAME = "sbq-aspiredaprdemo";
    const string COSMOS_DB_NAME = "cdb-aspiredaprdemo";
    const string COSMOS_DB_CONTAINER_NAME = "cdc-aspiredaprdemo";
    const string KEYVAULT_NAME = "kv-aspiredaprdemo";

    // Add supporting technologies in Azure
    var keyvault = builder.AddAzureKeyVault(KEYVAULT_NAME);
    var cosmosdb = builder.AddAzureCosmosDB("c-aspiredaprdemo");
    cosmosdb.AddCosmosDatabase(COSMOS_DB_NAME)
        .AddContainer(COSMOS_DB_CONTAINER_NAME, "/id");
    var serviceBus = builder.AddAzureServiceBus(SERVICE_BUS_NAME)
        .AddServiceBusQueue(QUEUE_NAME);

    // Configure Dapr.io components in Azure
    builder.AddBicepTemplate("dapr", "./dapr/dapr.bicep")
        .WithParameter("managedEnvironmentName", builder.Environment.EnvironmentName)
        .WithParameter("clientId", clientId)
        .WithParameter("keyVaultName", KEYVAULT_NAME)
        .WithParameter("messagingNamespace", $"{SERVICE_BUS_NAME}.servicebus.windows.net")
        .WithParameter("cosmosUrl", cosmosdb.GetOutput("connectionString"))
        .WithParameter("cosmosDatabaseName", COSMOS_DB_NAME)
        .WithParameter("cosmosCollectionName", COSMOS_DB_CONTAINER_NAME);

    // containerize our apps and services
    notificationService.PublishAsDockerFile();
    workflowService.PublishAsDockerFile();
    subscriberService.PublishAsDockerFile();
    weatherService.PublishAsDockerFile();
    frontend.PublishAsDockerFile();
}

builder.Build().Run();

