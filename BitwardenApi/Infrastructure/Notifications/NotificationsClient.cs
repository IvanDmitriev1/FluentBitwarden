using BitwardenApi.Contracts;
using BitwardenApi.Infrastructure.Serialization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization.Metadata;

namespace BitwardenApi.Infrastructure.Notifications;

internal sealed class NotificationsClient(
    ISignalRAccessTokenProvider accessTokenProvider,
    IBitwardenEnvironmentAccessor environmentAccessor,
    IServiceProvider serviceProvider)
    : INotificationsClient
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

        connection.On<NotificationEnvelope>("ReceiveMessage", DispatchAsync);

        return connection;
    }

    public Task DispatchAsync(NotificationEnvelope notificationEnvelope) =>
        notificationEnvelope.Type switch
        {
            NotificationType.SyncCipherCreate => PublishAsync(notificationEnvelope.Payload, BitwardenApiJsonContext.ConfiguredDefault.CipherChangedNotification),
            NotificationType.SyncCipherUpdate => PublishAsync(notificationEnvelope.Payload, BitwardenApiJsonContext.ConfiguredDefault.CipherChangedNotification),
            NotificationType.SyncCipherDelete => PublishAsync(notificationEnvelope.Payload, BitwardenApiJsonContext.ConfiguredDefault.CipherChangedNotification),

            NotificationType.SyncFolderCreate => PublishAsync(notificationEnvelope.Payload, BitwardenApiJsonContext.ConfiguredDefault.FolderChangedNotification),
            NotificationType.SyncFolderUpdate => PublishAsync(notificationEnvelope.Payload, BitwardenApiJsonContext.ConfiguredDefault.FolderChangedNotification),
            NotificationType.SyncFolderDelete => PublishAsync(notificationEnvelope.Payload, BitwardenApiJsonContext.ConfiguredDefault.FolderChangedNotification),

            NotificationType.SyncVault
                or NotificationType.SyncCiphers
                or NotificationType.SyncOrgKeys
                or NotificationType.SyncSettings
                or NotificationType.SyncOrganizations
                or NotificationType.SyncOrganizationStatusChanged
                or NotificationType.SyncOrganizationCollectionSettingChanged
                or NotificationType.SyncPolicy =>
                PublishAsync(notificationEnvelope.Payload, BitwardenApiJsonContext.ConfiguredDefault.VaultSyncRequestedNotification),

            _ => Task.CompletedTask,
        };

    private Task PublishAsync<TNotification>(
        JsonElement json,
        JsonTypeInfo<TNotification> jsonTypeInfo)
    {
        var notification = json.Deserialize(jsonTypeInfo) ?? throw new InvalidOperationException();
        var handler = serviceProvider.GetRequiredService<INotificationHandler<TNotification>>();

        return handler.HandleAsync(notification);
    }
}
