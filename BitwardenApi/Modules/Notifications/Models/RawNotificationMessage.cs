namespace BitwardenApi.Modules.Notifications.Models;

public sealed record RawNotificationMessage(
    string Type,
    string RawJson,
    DateTimeOffset ReceivedAtUtc);
