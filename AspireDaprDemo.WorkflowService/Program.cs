using System.Text.Json;

using AspireDaprDemo.ServiceDefaults;
using AspireDaprDemo.ServiceDefaults.SharedContracts;

using Dapr.Client;

using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();

app.UseCors();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost($"democronjob", async ([FromServices] DaprClient daprClient) =>
{
    const int PAGE_SIZE = 50;
    PagedCollection<Subscriber>? subscribers = null;
    while (subscribers == null || subscribers!.Items.Any()) // let's iterate subscribers in batches of 50 to send out notifications
    {
        var queryString = $"?pageSize={PAGE_SIZE}";
        if (subscribers?.PageIterationToken?.Length > 0)
        {
            queryString += $"&pageToken={subscribers.PageIterationToken}";
        }

        subscribers = await daprClient.InvokeMethodAsync<PagedCollection<Subscriber>>(HttpMethod.Get, GlobalConstants.AppIds.SubscriberService, queryString);
        IEnumerable<WeatherForecast> fiveDayForecast = await daprClient.InvokeMethodAsync<IEnumerable<WeatherForecast>>(HttpMethod.Get, GlobalConstants.AppIds.WeatherService, "/");
        WeatherForecast currentWeather = fiveDayForecast.First();

        foreach (var subscriber in subscribers.Items) // in real life we would probably want to build another notification endpoint that could accept an array of TemplatedEmailNotification.
        {
            TemplatedEmailNotification notification = new(
                subscriber.Email,
                "Weather Update Notification",
                "CURRENT_WEATHER_1",
                JsonDocument.Parse(JsonSerializer.Serialize(currentWeather)));
            await daprClient.PublishEventAsync(GlobalConstants.PubSubName, GlobalConstants.EventTopics.NotifyEmailTopicName, notification);
            //await daprClient.InvokeMethodAsync(GlobalConstants.AppIds.NotificationService, "/", notification);
        }
    }
}).WithCronJob()
.WithOpenApi(op =>
{
    op.OperationId = "SendWeatherSummaries";
    op.Summary = "Endpoint for CRON Job sending weather summaries to subscribers.";
    op.Description = "Sends weather summaries to subscribers when run.";

    return op;
});


await app.RunAsync();

public partial class Program
{
    protected Program() { }
}