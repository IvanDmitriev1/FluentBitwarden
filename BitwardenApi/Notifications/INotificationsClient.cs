namespace BitwardenApi.Notifications;

public interface INotificationsClient : IAsyncDisposable
{
    event EventHandler<RawNotificationMessage>? OnEvent;

    Task ConnectAsync(
        ConnectNotificationsRequest request,
        CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);
}
