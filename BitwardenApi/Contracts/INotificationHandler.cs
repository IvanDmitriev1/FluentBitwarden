namespace BitwardenApi.Contracts;

public interface INotificationHandler<in TNotification>
{
    Task HandleAsync(TNotification notification);
}