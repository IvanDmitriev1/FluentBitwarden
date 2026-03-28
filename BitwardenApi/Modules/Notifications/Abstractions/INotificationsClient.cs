using BitwardenApi.Modules.Notifications.Models;

namespace BitwardenApi.Modules.Notifications.Abstractions;

public interface INotificationsClient : IAsyncDisposable
{
    event EventHandler<RawNotificationMessage>? OnEvent;

    Task ConnectAsync(
        ConnectNotificationsRequest request,
        CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);
}
