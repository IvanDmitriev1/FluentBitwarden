namespace BitwardenApi.Modules.Notifications.Abstractions;

public interface INotificationsClient : IAsyncDisposable
{
    Task ConnectAsync(
        CancellationToken cancellationToken = default);

    Task DisconnectAsync();
}
