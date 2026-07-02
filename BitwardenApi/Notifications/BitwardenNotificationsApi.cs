using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization.Metadata;

namespace BitwardenApi.Notifications;

internal sealed class BitwardenNotificationsApi(
    IBitwardenAccessTokenProvider accessTokenProvider,
    IServiceProvider serviceProvider)
    : IBitwardenNotificationsApi
{
    private HubConnection? _connection;

    public async Task ConnectAsync(BitwardenAccountContext accountContext, CancellationToken cancellationToken = default)
    {
        if (_connection?.State is HubConnectionState.Connected
            or HubConnectionState.Connecting
            or HubConnectionState.Reconnecting)
        {
            return;
        }

        Uri requestedHubEndpoint = new(accountContext.Environment.NotificationsBase, "/hub");
        _connection = CreateConnection(requestedHubEndpoint, accountContext);

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

    private HubConnection CreateConnection(
        Uri hubEndpoint,
        BitwardenAccountContext accountContext)
    {
        HubConnection connection = new HubConnectionBuilder()
            .WithUrl(hubEndpoint, options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    AccessToken accessToken = await accessTokenProvider.GetAccessTokenAsync(
                        accountContext,
                        CancellationToken.None);

                    return accessToken.ToString();
                };
            })
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions = NotificationsJsonContext.ConfiguredDefault.Options;
            })
            .WithAutomaticReconnect()
            .Build();

        connection.On<NotificationEnvelope>("ReceiveMessage", DispatchAsync);

        return connection;
    }

    public Task DispatchAsync(NotificationEnvelope notificationEnvelope) =>
        notificationEnvelope.Type switch
        {
            NotificationType.SyncCipherCreate => PublishAsync(notificationEnvelope.Payload, NotificationsJsonContext.ConfiguredDefault.CipherChangedNotification),
            NotificationType.SyncCipherUpdate => PublishAsync(notificationEnvelope.Payload, NotificationsJsonContext.ConfiguredDefault.CipherChangedNotification),
            NotificationType.SyncCipherDelete => PublishAsync(notificationEnvelope.Payload, NotificationsJsonContext.ConfiguredDefault.CipherChangedNotification),

            NotificationType.SyncFolderCreate => PublishAsync(notificationEnvelope.Payload, NotificationsJsonContext.ConfiguredDefault.FolderChangedNotification),
            NotificationType.SyncFolderUpdate => PublishAsync(notificationEnvelope.Payload, NotificationsJsonContext.ConfiguredDefault.FolderChangedNotification),
            NotificationType.SyncFolderDelete => PublishAsync(notificationEnvelope.Payload, NotificationsJsonContext.ConfiguredDefault.FolderChangedNotification),

            NotificationType.SyncVault
                or NotificationType.SyncCiphers
                or NotificationType.SyncOrgKeys
                or NotificationType.SyncSettings
                or NotificationType.SyncOrganizations
                or NotificationType.SyncOrganizationStatusChanged
                or NotificationType.SyncOrganizationCollectionSettingChanged
                or NotificationType.SyncPolicy =>
                PublishAsync(notificationEnvelope.Payload, NotificationsJsonContext.ConfiguredDefault.VaultSyncRequestedNotification),

            _ => Task.CompletedTask,
        };

    private Task PublishAsync<TNotification>(JsonElement json, JsonTypeInfo<TNotification> jsonTypeInfo)
    {
        var notification = json.Deserialize(jsonTypeInfo) ?? throw new InvalidOperationException();
        var handler = serviceProvider.GetRequiredService<INotificationHandler<TNotification>>();

        return handler.HandleAsync(notification);
    }
}

