namespace BitwardenApi.Notifications.Models;

public readonly record struct CipherChangedNotification(CipherId Id, UserId UserId, bool IsDeleted);
