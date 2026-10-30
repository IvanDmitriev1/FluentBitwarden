
namespace BitwardenApi.Notifications.Models;

public readonly record struct PremiumStatusChangedNotification(UserId UserId, bool Premium);
