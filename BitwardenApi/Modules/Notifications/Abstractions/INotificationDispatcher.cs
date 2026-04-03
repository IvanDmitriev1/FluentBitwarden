using BitwardenApi.Modules.Notifications.Models;

namespace BitwardenApi.Modules.Notifications.Abstractions;

internal interface INotificationDispatcher
{
    Task DispatchAsync(in NotificationEnvelope notificationEnvelope, CancellationToken cancellationToken);
}