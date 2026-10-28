using BitwardenApi.Client;

namespace BitwardenApi.Notifications.Abstractions;

public interface IServerNotificationsClientFactory
{
    IServerNotificationsClient CreateClient(IAccessTokenProvider accessTokenProvider);
}
