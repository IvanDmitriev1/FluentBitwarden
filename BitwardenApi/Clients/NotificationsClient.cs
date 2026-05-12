using BitwardenApi.Contracts;
using BitwardenApi.Internal.Notifications;
using BitwardenApi.Models;
using BitwardenApi.Internal.Serialization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace BitwardenApi.Clients;

internal sealed class NotificationsClient(
    ISignalRAccessTokenProvider accessTokenProvider,
    INotificationDispatcher notificationDispatcher,
    IBitwardenEnvironmentAccessor environmentAccessor) : INotificationsClient
{
    private HubConnection? _connection;

    public async Task ConnectAsync(
        CancellationToken cancellationToken = default)
    {
        if (_connection?.State is HubConnectionState.Connected
            or HubConnectionState.Connecting
            or HubConnectionState.Reconnecting)
        {
            return;
        }

        BitwardenEnvironment environment = environmentAccessor.CurrentEnvironment;
        Uri requestedHubEndpoint = new(environment.NotificationsBase, "/hub");
        _connection = CreateConnection(requestedHubEndpoint);

        await _connection.StartWithRetryAsync(cancellationToken);
    }

    public async Task DisconnectAsync()
    {
        if (_connection is null)
            return;

        try
        {
            if (_connection.State != HubConnectionState.Disconnected)
            {
                await _connection.StopAsync();
            }
        }
        finally
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }

    private HubConnection CreateConnection(Uri hubEndpoint)
    {
        HubConnection connection = new HubConnectionBuilder()
            .WithUrl(hubEndpoint, options =>
            {
                options.AccessTokenProvider = accessTokenProvider.GetAccessToken;
            })
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions = BitwardenApiJsonContext.ConfiguredDefault.Options;
            })
            .WithAutomaticReconnect()
            .Build();

        connection.On<NotificationEnvelope>("ReceiveMessage", payload => notificationDispatcher.DispatchAsync(payload, CancellationToken.None));

        return connection;
    }
}
