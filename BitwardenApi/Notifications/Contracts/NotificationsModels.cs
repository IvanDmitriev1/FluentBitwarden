namespace BitwardenApi.Notifications.Contracts;

public readonly record struct VaultSyncRequestedNotification(
    UserId UserId);

public readonly record struct CipherChangedNotification(
    CipherId Id,
    UserId UserId,
    bool IsDeleted);

public readonly record struct FolderChangedNotification(
    FolderId Id,
    UserId UserId,
    bool IsDeleted);

public readonly record struct NotificationsReconnectedNotification(UserId UserId);