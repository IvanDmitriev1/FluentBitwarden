
namespace BitwardenApi.Notifications.Models;

public readonly record struct FolderChangedNotification(FolderId Id, UserId UserId, DateTimeOffset RevisionDate);
