namespace BitwardenApi.Contracts;

public interface INotificationsClient : IAsyncDisposable
{
    Task ConnectAsync(
        CancellationToken cancellationToken = default);

    Task DisconnectAsync();
}
