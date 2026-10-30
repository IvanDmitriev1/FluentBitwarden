using BitwardenApi.Client;
namespace BitwardenApi.Notifications.Abstractions;

public interface IServerNotificationsClient : IAsyncDisposable
{
    Task ConnectAsync(BitwardenAccountContext accountContext);
    ValueTask DisconnectAsync();
}
