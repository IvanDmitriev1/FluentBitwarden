namespace BitwardenApi.Notifications;

public interface INotificationsApi : IAsyncDisposable
{
    Task ConnectAsync(
        CancellationToken cancellationToken = default);

    Task DisconnectAsync();
}
