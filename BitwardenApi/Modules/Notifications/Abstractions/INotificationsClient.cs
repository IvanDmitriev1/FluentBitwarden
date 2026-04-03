namespace BitwardenApi.Modules.Notifications.Abstractions;

public interface INotificationsClient : IAsyncDisposable
{
    Task ConnectAsync(
        BitwardenEnvironment environment,
        CancellationToken cancellationToken = default);

    Task DisconnectAsync();
}
