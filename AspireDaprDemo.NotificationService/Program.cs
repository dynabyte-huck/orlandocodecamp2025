using AspireDaprDemo.NotificationService;
using AspireDaprDemo.ServiceDefaults;
using AspireDaprDemo.ServiceDefaults.SharedContracts;

using Dapr.Client;

using MailKit.Net.Smtp;
using MailKit.Security;

using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

using MimeKit;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var app = builder.Build();

app.UseCors();

app.MapDefaultEndpoints();

// Dapr will send serialized event object vs. being raw CloudEvent
app.UseCloudEvents();

// needed for Dapr pub/sub routing
app.MapSubscribeHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/template", async ([FromServices] DaprClient daprClient, int pageSize = 10, string? pageToken = "") =>
{
    if (pageSize <= 0)
    {
        return Results.BadRequest("PageSize must be greater than zero.");
    }

    var query = $$"""
            {
                "page": {
                    "limit": {{pageSize}},
                    "token": "{{pageToken}}"
                }
            }
            """;

    var data = await daprClient.QueryStateAsync<string>(GlobalConstants.StateStoreName, query);
    var result = new PagedCollection<NotificationTemplate>(data.Results.Where(r => r.Data != null).Select(r => new NotificationTemplate(r.Key, r.Data!)).ToList())
    {
        PageSize = pageSize,
        PageIterationToken = data.Token
    };
    return Results.Ok(result);
}).WithOpenApi(op =>
{
    op.OperationId = "GetTemplates";
    op.Summary = "Retrieve a paginated list of notification templates";
    op.Description = "Fetch a list of notification templates from the state store with support for pagination.";
    op.Parameters[0].Description = "The number of items to return per page. Must be greater than zero.";
    op.Parameters[1].Description = "The token to continue fetching items from the previous page.";
    return op;
});

app.MapGet("/template/{id}", async ([FromServices] DaprClient daprClient, string id) =>
{
    var result = await daprClient.GetStateAsync<string>(GlobalConstants.StateStoreName, id);

    if (string.IsNullOrWhiteSpace(result))
    {
        var error = new ProblemDetails() { Detail = $"No template could be found with for ID {id}.", Status = StatusCodes.Status404NotFound, Title = "No template found." };
        return Results.Json(error, statusCode: StatusCodes.Status404NotFound);
    }

    return Results.Ok(new NotificationTemplate(id, result));
}).WithOpenApi(op =>
{
    op.OperationId = "GetTemplateById";
    op.Summary = "Retrieve a notification template by ID";
    op.Description = "Fetch a specific notification template from the state store using its unique ID.";
    op.Parameters[0].Description = "The unique identifier of the notification template to retrieve.";
    return op;
});

app.MapPost("/template/{id}", async ([FromServices] DaprClient daprClient, string id, HttpContext context) =>
{
    if (!context.Request.ContentType?.StartsWith("text/plain") ?? true)
    {
        var error = new ProblemDetails() { Detail = $"/templates/{id} only supports text/plain content types.", Status = StatusCodes.Status400BadRequest, Title = "Content Type not supported." };
        return Results.Json(error, statusCode: StatusCodes.Status400BadRequest);
    }

    using var reader = new StreamReader(context.Request.Body);
    var template = await reader.ReadToEndAsync();
    var result = await daprClient.GetStateAsync<string>(GlobalConstants.StateStoreName, id);

    if (!string.IsNullOrWhiteSpace(result))
    {
        var error = new ProblemDetails() { Detail = $"A template already exists at ID {id}. To update a template, please use PUT.", Status = StatusCodes.Status409Conflict, Title = "Template exists." };
        return Results.Json(error, statusCode: StatusCodes.Status409Conflict);
    }

    await daprClient.SaveStateAsync(GlobalConstants.StateStoreName, id, template, metadata: Metadata);
    return Results.Ok();
}).WithOpenApi(op =>
{
    op.OperationId = "CreateTemplate";
    op.Summary = "Create a new notification template";
    op.Description = "Creates a new notification template with the specified ID. The request body must contain the template content as plain text.";
    op.Parameters.Add(new OpenApiParameter
    {
        Name = "id",
        In = ParameterLocation.Path,
        Required = true,
        Description = "The unique identifier for the new notification template.",
        Schema = new OpenApiSchema
        {
            Type = "string"
        }
    });
    op.RequestBody = new OpenApiRequestBody
    {
        Description = "The email template to create",
        Required = true
    };
    op.Responses["200"] = new OpenApiResponse
    {
        Description = "Template created successfully."
    };
    op.Responses["400"] = new OpenApiResponse
    {
        Description = "A template with the specified ID already exists."
    };
    op.Responses["404"] = new OpenApiResponse
    {
        Description = "Content type must be text/plain."
    };
    return op;
});

app.MapPut("/template/{id}", async ([FromServices] DaprClient daprClient, string id, HttpContext context) =>
{
    if (!context.Request.ContentType?.StartsWith("text/plain") ?? true)
    {
        var problem = new ProblemDetails() { Detail = $"/templates/{id} only supports text/plain content types.", Status = StatusCodes.Status404NotFound, Title = "Invalid content type." };
        return Results.Json(problem, statusCode: StatusCodes.Status404NotFound);
    }

    using var reader = new StreamReader(context.Request.Body);
    var template = await reader.ReadToEndAsync();
    var result = await daprClient.GetStateAsync<string>(GlobalConstants.StateStoreName, id);

    if (string.IsNullOrWhiteSpace(result))
    {
        var problem = new ProblemDetails() { Detail = $"A template does not exist at ID {id}. To create a new template, please use POST.", Status = StatusCodes.Status400BadRequest, Title = "Invalid request." };
        return Results.Json(problem, statusCode: StatusCodes.Status400BadRequest);
    }

    await daprClient.DeleteStateAsync(GlobalConstants.StateStoreName, id);
    await daprClient.SaveStateAsync(GlobalConstants.StateStoreName, id, template, metadata: Metadata);
    return Results.Ok();
}).WithOpenApi(op =>
{
    op.OperationId = "UpdateTemplate";
    op.Summary = "Update an existing notification template";
    op.Description = "Updates the content of an existing notification template identified by the specified ID. The template content must be provided as plain text.";
    op.Parameters[0].Description = "The unique identifier of the notification template to update.";
    op.RequestBody = new OpenApiRequestBody
    {
        Description = "The updated content of the notification template in plain text.",
        Required = true,
    };
    return op;
});

app.MapDelete("/template/{id}", async ([FromServices] DaprClient daprClient, string id) =>
{
    var result = await daprClient.GetStateAsync<string>(GlobalConstants.StateStoreName, id);

    if (string.IsNullOrWhiteSpace(result))
    {
        var error = new ProblemDetails() { Detail = $"A template does not exist at ID {id}.", Status = StatusCodes.Status400BadRequest, Title = "Template does not exist" };
        return Results.Json(error, statusCode: StatusCodes.Status400BadRequest);
    }

    await daprClient.DeleteStateAsync(GlobalConstants.StateStoreName, id);
    return Results.Ok();
}).WithOpenApi(op =>
{
    op.OperationId = "DeleteTemplate";
    op.Summary = "Delete an existing notification template";
    op.Description = "Deletes the notification template identified by the specified ID from the state store.";
    op.Parameters[0].Description = "The unique identifier of the notification template to delete.";
    return op;
});

app.MapPost("/notifyemail", async ([FromServices] DaprClient daprClient, [FromServices] ILogger<Program> logger, [FromBody] TemplatedEmailNotification notification) =>
{
    var template = await daprClient.GetStateAsync<string>(GlobalConstants.StateStoreName, notification.TemplateId);
    if (string.IsNullOrWhiteSpace(template))
    {
        var error = new ProblemDetails() { Detail = $"A template does not exist at ID {notification.TemplateId}.", Status = StatusCodes.Status400BadRequest, Title = "Template does not exist" };
        logger.LogError(message: "A template does not exist at ID {notificationTemplateId}.", notification.TemplateId);
        return Results.Json(error, statusCode: StatusCodes.Status400BadRequest);
    }

    var emailBody = TemplateDataFormatter.ReplaceTemplateWithJsonValues(notification.Data, template);

    string subType = "plain";
    if (template!.ToLowerInvariant().Contains("</html>"))
    {
        subType = "html";
    }

    Dictionary<string, string> smtpHostSecret = await daprClient.GetSecretAsync(GlobalConstants.SecretStoreName, "smtp-host");
    Dictionary<string, string> smtpPortSecret = await daprClient.GetSecretAsync(GlobalConstants.SecretStoreName, "smtp-port");
    Dictionary<string, string> smtpApiKeySecret = await daprClient.GetSecretAsync(GlobalConstants.SecretStoreName, "smtp-apiKey");
    string smtpHost = smtpHostSecret["smtp-host"];
    int smtpPort = int.Parse(smtpPortSecret["smtp-port"]);
    string smtpApiKey = smtpApiKeySecret["smtp-apiKey"];

    var email = new MimeMessage();
    email.From.Add(MailboxAddress.Parse("donotreply-weather@dynabytetech.com"));
    email.To.Add(MailboxAddress.Parse(notification.ToAddress));
    email.Subject = notification.Subject;
    email.Body = new TextPart(subType) { Text = emailBody };

    using var smtp = new SmtpClient();
    await smtp.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTlsWhenAvailable);
    if (!string.IsNullOrWhiteSpace(smtpApiKey))
    {
        await smtp.AuthenticateAsync("apikey", smtpApiKey);
    }

    await smtp.SendAsync(email);

    return Results.Ok();
}).WithTopic(GlobalConstants.PubSubName, GlobalConstants.EventTopics.NotifyEmailTopicName)
.WithOpenApi(op =>
{
    op.OperationId = "SendTemplatedEmail";
    op.Summary = "Send a templated email notification";
    op.Description = "Sends an email using a pre-defined template by replacing placeholders with provided data. The email content, recipient address, and subject are generated based on the template and provided metadata.";
    op.RequestBody = new OpenApiRequestBody
    {
        Description = "The details of the email notification, including the template ID, recipient address, subject, and data for template replacement.",
        Required = true
    };
    return op;
});

await app.RunAsync();

public partial class Program
{
    protected Program() { }

    static Dictionary<string, string> Metadata => new() { { "contentType", "application/json" } };
}