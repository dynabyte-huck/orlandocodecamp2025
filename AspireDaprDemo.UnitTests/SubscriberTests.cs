extern alias SubscriberService;

using System.Net.Http.Json;
using System.Text.Json;

using AspireDaprDemo.ServiceDefaults;
using AspireDaprDemo.ServiceDefaults.SharedContracts;

using Dapr.Client;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

using Moq;

namespace AspireDaprDemo.UnitTests;
public class SubscriberServiceTests : IClassFixture<WebApplicationFactory<SubscriberService::Program>>
{
    private readonly HttpClient _client;
    private readonly Mock<DaprClient> _mockDaprClient;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public SubscriberServiceTests(WebApplicationFactory<SubscriberService::Program> factory)
    {
        _mockDaprClient = new Mock<DaprClient>();
        _jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(_mockDaprClient.Object);
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        Environment.SetEnvironmentVariable("APP_ID", GlobalConstants.AppIds.SubscriberService);
    }

    [Fact]
    public async Task CreateSubscriber_ShouldNotSaveDuplicateEmail()
    {
        // Arrange
        var newSubscriber = new Subscriber(Guid.NewGuid().ToString(), "test@subscriber.com", "Test", "Subscriber");

        var stateQueryResponse = new StateQueryResponse<AppStateEntry<Subscriber>>(
            [
            new StateQueryItem<AppStateEntry<Subscriber>>(
                Guid.NewGuid().ToString(),
                new AppStateEntry<Subscriber>(GlobalConstants.AppIds.SubscriberService,
                    new Subscriber(Guid.NewGuid().ToString(), newSubscriber.Email, "Some", "Subscriber")),
                "etag",
                "")
            ], "Token", new Dictionary<string, string>());

        _mockDaprClient.Setup(x => x.QueryStateAsync<AppStateEntry<Subscriber>>(GlobalConstants.StateStoreName, It.IsAny<string>(), null, default))
            .Returns(Task.FromResult(stateQueryResponse));

        // Act
        var response = await _client.PostAsJsonAsync($"/{newSubscriber.SubscriberId}", newSubscriber);
        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<ProblemDetails>(responseContent, _jsonSerializerOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseObject.Should().NotBeNull();
        responseObject!.Detail.Should().Be("The subscriber has validation errors.");
        responseObject!.Extensions.Should().ContainKey("validationErrors");
        responseObject.Extensions["validationErrors"].Should().NotBeNull();
    }

    [Fact]
    public async Task CreateSubscriber_ShouldSaveValidSubscriber()
    {
        // Arrange
        var newSubscriber = new Subscriber(Guid.NewGuid().ToString(), "newtest@subscriber.com", "Test", "Subscriber");

        // Act
        var response = await _client.PostAsJsonAsync($"/{newSubscriber.SubscriberId}", newSubscriber);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        _mockDaprClient.Verify(x =>
            x.SaveStateAsync(GlobalConstants.StateStoreName,
                newSubscriber.SubscriberId,
                It.Is<AppStateEntry<Subscriber>>(e => e.Entity.SubscriberId == newSubscriber.SubscriberId),
                null,
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                default));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}