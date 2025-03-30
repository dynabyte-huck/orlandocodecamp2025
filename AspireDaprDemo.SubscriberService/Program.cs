using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

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

app.MapGet("/", async ([FromServices] DaprClient daprClient, int pageSize = 10, string? pageToken = "") =>
{
    if (pageSize <= 0)
    {
        return Results.BadRequest("PageSize must be greater than zero.");
    }

    var appId = GlobalConstants.AppIds.SubscriberService;

    var query = $$"""
        {
            "page": {
                "limit": {{pageSize}},
                "token": "{{pageToken}}"
            },
            "filter": {
                "EQ": { "appId": "{{appId}}" }
            }
        }
        """;

    var data = await daprClient.QueryStateAsync<AppStateEntry<Subscriber>>(GlobalConstants.StateStoreName, query);
    var result = new PagedCollection<Subscriber>(data.Results.Where(r => r.Data?.Entity != null).Select(r => r.Data!.Entity).ToList())
    {
        PageSize = pageSize,
        PageIterationToken = data.Token
    };
    return Results.Ok(result);
}).WithOpenApi(op =>
{
    op.OperationId = "GetSubscribers";
    op.Summary = "Retrieve a paginated list of subscribers";
    op.Description = "Fetch a list of subscribers from the state store with support for pagination.";
    op.Parameters[0].Description = "The number of items to return per page. Must be greater than zero.";
    op.Parameters[1].Description = "The token to continue fetching items from the previous page.";
    return op;
});

app.MapGet("/{id}", async ([FromServices] DaprClient daprClient, string id) =>
{
    var result = await daprClient.GetStateAsync<string>(GlobalConstants.StateStoreName, id);

    if (string.IsNullOrWhiteSpace(result))
    {
        var error = new ProblemDetails() { Detail = $"No subscriber could be found with for ID {id}.", Status = StatusCodes.Status404NotFound, Title = "No template found." };
        return Results.Json(error, statusCode: StatusCodes.Status404NotFound);
    }

    return Results.Ok(new NotificationTemplate(id, result));
}).Produces<ProblemDetails>(StatusCodes.Status404NotFound)
.WithOpenApi(op =>
{
    op.OperationId = "GetSubscriberById";
    op.Summary = "Retrieve a subscriber by ID";
    op.Description = "Fetch a specific subscriber from the state store using its unique ID.";
    op.Parameters[0].Description = "The unique identifier of the subscriber to retrieve.";

    op.Responses["404"] = new Microsoft.OpenApi.Models.OpenApiResponse
    {
        Description = "A subscriber does not exist with the given ID.",
        Content = OpenApiHelpers.GetJsonContentMediaType<ProblemDetails>()
    };

    return op;
});

app.MapPost("/{id}", async ([FromServices] DaprClient daprClient,
    string id,
    [FromBody] Subscriber subscriber) =>
{
    if (subscriber == null)
    {
        var error = new ProblemDetails() { Detail = "An empty subscriber cannot be created.", Status = StatusCodes.Status400BadRequest, Title = "Empty Subscriber" };
        return Results.Json(error, statusCode: StatusCodes.Status400BadRequest);
    }

    var result = await daprClient.GetStateAsync<AppStateEntry<Subscriber>>(GlobalConstants.StateStoreName, id);

    if (result != null)
    {
        var error = new ProblemDetails() { Detail = $"A subscriber already exists at ID {id}. To update a subscriber, please use PUT.", Status = StatusCodes.Status409Conflict, Title = "Subscriber Already Exists" };
        return Results.Json(error, statusCode: StatusCodes.Status409Conflict);
    }

    var query = $$"""
        {
            "filter": {
                "AND": [{
                    "EQ": { "appId": "{{GlobalConstants.AppIds.SubscriberService}}" }
                }, {
                    "EQ": { "entity.email": "{{subscriber.Email}}" }
                }]
            }
        }
        """;

    var queryResult = await daprClient.QueryStateAsync<AppStateEntry<Subscriber>>(GlobalConstants.StateStoreName, query);

    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(subscriber);

    if (!Validator.TryValidateObject(subscriber, validationContext, validationResults, true) || queryResult.Results.Any())
    {
        if (queryResult != null)
        {
            validationResults.Add(new ValidationResult($"The email {subscriber.Email} is already in use by a subscriber. Subscriber emails must be unique."));
        }

        var errors = validationResults.Select(vr => new
        {
            Field = vr.MemberNames.FirstOrDefault(),
            Message = vr.ErrorMessage
        });

        var error = new ProblemDetails() { Detail = "The subscriber has validation errors.", Status = StatusCodes.Status400BadRequest, Title = "Subscriber Validation Errors" };
        error.Extensions.Add("validationErrors", errors);

        return Results.Json(error, statusCode: StatusCodes.Status400BadRequest);
    }

    // we use AppStateEntry to save the app id becuase query is global to the state store so we need to filter by it
    var entry = new AppStateEntry<Subscriber>(GlobalConstants.AppIds.SubscriberService, subscriber);

    await daprClient.SaveStateAsync(GlobalConstants.StateStoreName, id, entry, metadata: Metadata);
    return Results.Ok();
})
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status409Conflict)
.WithOpenApi(op =>
{
    op.OperationId = "CreateSubscriber";
    op.Summary = "Create a new subscriber by ID";
    op.Description = "Saves a new subscriber in the state store using the provided unique ID. If a configuration with the same ID exists, it returns an error.";
    op.Parameters[0].Description = "The unique identifier for the new subscriber.";

    // Define the request body
    op.RequestBody = new Microsoft.OpenApi.Models.OpenApiRequestBody
    {
        Description = "The subscriber to be saved.",
        Required = true,
        Content = OpenApiHelpers.GetJsonContentMediaType<Subscriber>(),
    };

    // Optionally define the response if needed
    op.Responses["200"] = new Microsoft.OpenApi.Models.OpenApiResponse
    {
        Description = "The subscriber was successfully created."
    };
    op.Responses["400"] = new Microsoft.OpenApi.Models.OpenApiResponse
    {
        Description = "The provided subscriber is null or empty or there are validation errors.",
        Content = OpenApiHelpers.GetJsonContentMediaType<ProblemDetails>(),
    };
    op.Responses["409"] = new Microsoft.OpenApi.Models.OpenApiResponse
    {
        Description = "A subscriber with the Id already exists",
        Content = OpenApiHelpers.GetJsonContentMediaType<ProblemDetails>(),
    };

    return op;
});

app.MapPut("/{id}", async ([FromServices] DaprClient daprClient,
    [FromServices] ClaimsPrincipal principal,
    string id,
    [FromBody] Subscriber subscriber) =>
{
    if (subscriber == null)
    {
        var error = new ProblemDetails() { Detail = "A null or empty subscriber cannot be updated.", Status = StatusCodes.Status400BadRequest, Title = "Empty Subscriber" };
        return Results.Json(error, statusCode: StatusCodes.Status400BadRequest);
    }

    var result = await daprClient.GetStateAsync<AppStateEntry<Subscriber>>(GlobalConstants.StateStoreName, id);

    if (result == null)
    {
        var error = new ProblemDetails() { Detail = $"A subscriber does not exist at ID {id}. To create a new subscriber, please use POST.", Status = StatusCodes.Status404NotFound, Title = "Subscriber Not Found" };
        return Results.Json(error, statusCode: StatusCodes.Status404NotFound);
    }

    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(subscriber);

    if (!Validator.TryValidateObject(subscriber, validationContext, validationResults, true))
    {
        var errors = validationResults.Select(vr => new
        {
            Field = vr.MemberNames.FirstOrDefault(),
            Message = vr.ErrorMessage
        });

        var error = new ProblemDetails() { Detail = "The subscriber has validation errors.", Status = StatusCodes.Status400BadRequest, Title = "Subscriber Validation Errors" };
        error.Extensions.Add("validationErrors", errors);

        return Results.Json(error, statusCode: StatusCodes.Status400BadRequest);
    }

    await daprClient.DeleteStateAsync(GlobalConstants.StateStoreName, id);
    await daprClient.SaveStateAsync(GlobalConstants.StateStoreName, id, subscriber, metadata: Metadata);
    return Results.Ok();
})
.Produces<ProblemDetails>(StatusCodes.Status404NotFound)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.WithOpenApi(op =>
{
    op.OperationId = "UpdateSubscriber";
    op.Summary = "Update an existing subscriber by ID";
    op.Description = "Updates an existing subscriber in the state store using the provided unique ID. If a subscriber does not exist for the ID, it returns an error.";
    op.Parameters[0].Description = "The unique identifier for the subscriber to update.";

    // Define the request body
    op.RequestBody = new Microsoft.OpenApi.Models.OpenApiRequestBody
    {
        Description = "The updated subscriber to be saved.",
        Required = true,
        Content = OpenApiHelpers.GetJsonContentMediaType<Subscriber>(),
    };

    // Define responses
    op.Responses["200"] = new Microsoft.OpenApi.Models.OpenApiResponse
    {
        Description = "The subscriber was successfully updated.",
        Content = OpenApiHelpers.GetJsonContentMediaType<ProblemDetails>()
    };
    op.Responses["400"] = new Microsoft.OpenApi.Models.OpenApiResponse
    {
        Description = "The provided subscriber is null or empty or there are validation errors.",
        Content = OpenApiHelpers.GetJsonContentMediaType<ProblemDetails>()
    };
    op.Responses["404"] = new Microsoft.OpenApi.Models.OpenApiResponse
    {
        Description = "A subscriber does not exist with the given ID.",
        Content = OpenApiHelpers.GetJsonContentMediaType<ProblemDetails>()
    };

    return op;
});


app.MapDelete("/{id}", async ([FromServices] DaprClient daprClient, string id) =>
{
    var result = await daprClient.GetStateAsync<string>(GlobalConstants.StateStoreName, id);

    if (string.IsNullOrWhiteSpace(result))
    {
        var error = new ProblemDetails() { Detail = $"A subscriber does not exist at ID {id}.", Status = StatusCodes.Status404NotFound, Title = "Subscriber does not exist" };
        return Results.Json(error, statusCode: StatusCodes.Status404NotFound);
    }

    await daprClient.DeleteStateAsync(GlobalConstants.StateStoreName, id);
    return Results.Ok();
}).WithOpenApi(op =>
{
    op.OperationId = "DeleteSubscriber";
    op.Summary = "Delete an existing subscriber";
    op.Description = "Deletes the subscriber identified by the specified ID from the state store.";
    op.Parameters[0].Description = "The unique identifier of the subscriber to delete.";

    op.Responses["404"] = new Microsoft.OpenApi.Models.OpenApiResponse
    {
        Description = "A subscriber does not exist with the given ID.",
        Content = OpenApiHelpers.GetJsonContentMediaType<ProblemDetails>()
    };

    return op;
});

await app.RunAsync();

public partial class Program
{
    protected Program() { }

    static Dictionary<string, string> Metadata => new() { { "contentType", "application/json" } };
}