namespace BitwardenApi.Notifications;

public sealed record RawNotificationMessage(
    string Type,
    string RawJson,
    DateTimeOffset ReceivedAtUtc);
