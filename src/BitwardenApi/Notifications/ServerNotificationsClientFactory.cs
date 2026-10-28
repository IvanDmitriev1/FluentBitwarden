using BitwardenApi.Client;
using BitwardenApi.Notifications.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace BitwardenApi.Notifications;

internal sealed class ServerNotificationsClientFactory(IServiceScopeFactory serviceScopeFactory, TimeProvider timeProvider) : IServerNotificationsClientFactory
{
    public IServerNotificationsClient CreateClient(IAccessTokenProvider accessTokenProvider)
    {
        return new ServerNotificationsClient(accessTokenProvider, serviceScopeFactory, timeProvider);
    }
}
