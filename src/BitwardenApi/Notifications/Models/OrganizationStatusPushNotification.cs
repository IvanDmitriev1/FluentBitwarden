
namespace BitwardenApi.Notifications.Models;

public readonly record struct OrganizationStatusPushNotification(OrganizationId OrganizationId, bool Enabled);
