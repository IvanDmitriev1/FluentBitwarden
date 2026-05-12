using BitwardenApi.Models;

namespace BitwardenApi.Internal.Notifications;

internal interface INotificationDispatcher
{
    Task DispatchAsync(in NotificationEnvelope notificationEnvelope, CancellationToken cancellationToken);
}
