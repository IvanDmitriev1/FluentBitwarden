namespace BitwardenApi.Notifications.Contracts;

internal readonly record struct NotificationEnvelope(
    ContextId ContextId,
    NotificationType Type,
    JsonElement Payload);

