using System.Text.Json;

namespace AspireDaprDemo.ServiceDefaults.SharedContracts;

public record NotificationTemplate(string TemplateId, string Template);

public record TemplatedEmailNotification(string ToAddress, string Subject, string TemplateId, JsonDocument Data);