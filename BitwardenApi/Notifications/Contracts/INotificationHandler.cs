namespace BitwardenApi.Notifications.Contracts;

public interface INotificationHandler<in TNotification>
{
    Task HandleAsync(TNotification notification);
}