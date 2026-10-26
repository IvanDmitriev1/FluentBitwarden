using BitwardenApi.Notifications.Models;

namespace BitwardenApi.Notifications.Internal;

internal readonly record struct NotificationEnvelope(
    ContextId ContextId,
    NotificationType Type,
    JsonElement Payload);

