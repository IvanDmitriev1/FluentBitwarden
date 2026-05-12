namespace BitwardenApi.Infrastructure.Notifications;

internal readonly record struct NotificationEnvelope(
    ContextId ContextId,
    NotificationType Type,
    JsonElement Payload);
