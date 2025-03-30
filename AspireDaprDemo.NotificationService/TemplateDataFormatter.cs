using System.Text.Json;
using System.Text.RegularExpressions;

namespace AspireDaprDemo.NotificationService;

public partial class TemplateDataFormatter
{
    public static string ReplaceTemplateWithJsonValues(JsonDocument jsonDocument, string template)
    {
        // Regular expression to match placeholders in the template
        var regex = JsonDataRegex();

        // Replace matches with corresponding JSON values
        var result = regex.Replace(template, match =>
        {
            var path = match.Groups[1].Value; // Extract path from placeholder
            var value = GetJsonValue(jsonDocument.RootElement, path);
            return value ?? match.Value; // If value is null, keep the placeholder
        });

        return result;
    }

    private static string? GetJsonValue(JsonElement element, string path)
    {
        var segments = path.Split('.'); // Split path into segments

        foreach (var segment in segments)
        {
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(segment, out var child))
            {
                element = child; // Navigate to the child property
            }
            else
            {
                return null; // Path not found
            }
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            _ => element.GetRawText() // Handle other cases (e.g., objects or arrays)
        };
    }

    [GeneratedRegex(@"\{([^{}]+)\}")]
    private static partial Regex JsonDataRegex();
}