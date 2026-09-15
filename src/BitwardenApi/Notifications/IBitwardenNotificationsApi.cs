namespace BitwardenApi.Notifications;

public interface IBitwardenNotificationsApi : IAsyncDisposable
{
    Task ConnectAsync(
        BitwardenAccountContext accountContext,
        CancellationToken cancellationToken = default);

    Task DisconnectAsync();
}
