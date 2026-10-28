
namespace BitwardenApi.Notifications.Models;

public readonly record struct AutoConfirmMemberNotification(UserId UserId, UserId TargetUserId, OrganizationId OrganizationId, string TargetOrganizationUserId);
