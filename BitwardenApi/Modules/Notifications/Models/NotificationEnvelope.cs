namespace BitwardenApi.Modules.Notifications.Models;

internal readonly record struct NotificationEnvelope(
    ContextId ContextId,
    NotificationType Type,
    JsonElement Payload);