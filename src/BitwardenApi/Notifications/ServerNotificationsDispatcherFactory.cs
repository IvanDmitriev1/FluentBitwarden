using BitwardenApi.Client;
using BitwardenApi.Notifications.Abstractions;
using BitwardenApi.Notifications.Internal;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace BitwardenApi.Notifications;

internal sealed class ServerNotificationsDispatcherFactory(
    IAccessTokenProvider accessTokenProvider,
    IServiceScopeFactory serviceScopeFactory,
    TimeProvider timeProvider) : IServerNotificationsDispatcherFactory
{
    public IServerNotificationsDispatcher CreateDispatcher(BitwardenAccountContext accountContext, Action<IHubConnectionBuilder>? configureConnection = null)
    {
        IHubConnectionBuilder connectionBuilder = CreateConnection(accountContext);
        configureConnection?.Invoke(connectionBuilder);

        var connection = connectionBuilder.Build();
        return new ServerNotificationsDispatcher(connection, serviceScopeFactory, timeProvider);
    }

    private IHubConnectionBuilder CreateConnection(BitwardenAccountContext accountContext)
    {
        IHubConnectionBuilder builder = new HubConnectionBuilder()
            .WithUrl(new Uri(accountContext.Environment.NotificationsBase, "/hub"), options =>
            {
                options.Transports = HttpTransportType.WebSockets;
                options.SkipNegotiation = true;

                options.AccessTokenProvider = async () =>
                {
                    SessionAccessToken sessionAccessToken =
                        await accessTokenProvider.GetAccessTokenAsync(accountContext);

                    return sessionAccessToken.ToString();
                };
            })
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions = NotificationsJsonContext.ConfiguredDefault.Options;
            })
            .WithAutomaticReconnect();

        return builder;
    }
}
