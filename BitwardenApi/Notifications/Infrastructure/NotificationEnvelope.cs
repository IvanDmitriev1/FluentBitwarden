namespace BitwardenApi.Notifications.Infrastructure;

internal readonly record struct NotificationEnvelope(
    ContextId ContextId,
    NotificationType Type,
    JsonElement Payload);

