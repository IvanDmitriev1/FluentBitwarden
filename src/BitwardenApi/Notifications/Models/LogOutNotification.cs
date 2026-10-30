
namespace BitwardenApi.Notifications.Models;

public readonly record struct LogOutNotification(UserId UserId, PushNotificationLogOutReasonType? Reason);
