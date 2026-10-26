namespace BitwardenApi.Notifications.Models;

public interface INotificationHandler<in TNotification>
{
    Task HandleAsync(TNotification notification);
}
