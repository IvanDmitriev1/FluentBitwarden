namespace BitwardenApi.Modules.Notifications.Abstractions;

public interface INotificationHandler<in TNotification>
{
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken);
}