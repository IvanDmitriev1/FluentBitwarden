
namespace BitwardenApi.Notifications.Models;

public readonly record struct SyncSendNotification(string Id, UserId UserId, DateTimeOffset RevisionDate);
