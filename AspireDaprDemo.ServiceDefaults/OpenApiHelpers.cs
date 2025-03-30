using Microsoft.OpenApi.Models;

namespace AspireDaprDemo.ServiceDefaults;

public static class OpenApiHelpers
{
    public static IDictionary<string, OpenApiMediaType> GetJsonContentMediaType<T>()
    {
        var content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Type = "object",
                    Reference = new OpenApiReference
                    {
                        Id = nameof(T),
                        Type = ReferenceType.Schema,
                    },
                },
            },
        };

        return content;
    }
}
