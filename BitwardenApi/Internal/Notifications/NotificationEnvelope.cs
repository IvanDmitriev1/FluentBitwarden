using BitwardenApi.Models;

namespace BitwardenApi.Internal.Notifications;

internal readonly record struct NotificationEnvelope(
    ContextId ContextId,
    NotificationType Type,
    JsonElement Payload);
