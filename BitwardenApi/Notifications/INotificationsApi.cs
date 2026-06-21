namespace BitwardenApi.Notifications;

public interface INotificationsApi : IAsyncDisposable
{
    Task ConnectAsync(
        BitwardenAccountContext accountContext,
        CancellationToken cancellationToken = default);

    Task DisconnectAsync();
}
