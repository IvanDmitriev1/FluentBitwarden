
namespace BitwardenApi.Notifications.Models;

public readonly record struct AuthRequestPushNotification(AuthRequestId Id, UserId UserId);
