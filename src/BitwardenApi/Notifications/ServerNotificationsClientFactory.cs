using BitwardenApi.Client;
using BitwardenApi.Notifications.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace BitwardenApi.Notifications;

internal sealed class ServerNotificationsClientFactory(
    IServiceScopeFactory serviceScopeFactory,
    IAccessTokenProvider accessTokenProvider,
    TimeProvider timeProvider) : IServerNotificationsClientFactory
{
    public IServerNotificationsClient CreateClient()
    {
        return new ServerNotificationsClient(accessTokenProvider, serviceScopeFactory, timeProvider);
    }
}
