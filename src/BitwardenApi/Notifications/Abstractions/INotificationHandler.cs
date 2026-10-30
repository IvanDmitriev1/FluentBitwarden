namespace BitwardenApi.Notifications.Abstractions;

public interface INotificationHandler<in TNotification>
{
    Task HandleAsync(
        NotificationType type,
        TNotification notification,
        CancellationToken cancellationToken = default);
}
