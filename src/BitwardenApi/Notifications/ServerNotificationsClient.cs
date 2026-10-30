using BitwardenApi.Client;
using BitwardenApi.Notifications.Abstractions;
using BitwardenApi.Notifications.Internal;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;

namespace BitwardenApi.Notifications;

internal sealed class ServerNotificationsClient(
    IAccessTokenProvider accessTokenProvider,
    IServiceScopeFactory serviceScopeFactory,
    TimeProvider timeProvider,
    Action<IHubConnectionBuilder>? configureConnection = null)
    : IServerNotificationsClient
{
    private readonly ResiliencePipeline _startRetry = new ResiliencePipelineBuilder
        {
            TimeProvider = timeProvider
        }
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = int.MaxValue,
            Delay = TimeSpan.FromSeconds(2),
            MaxDelay = TimeSpan.FromSeconds(30),
            BackoffType = DelayBackoffType.Exponential
        })
        .Build();

    private HubConnection? _connection;

    public async Task ConnectAsync(BitwardenAccountContext accountContext)
    {
        if (_connection?.State is HubConnectionState.Connected
            or HubConnectionState.Connecting
            or HubConnectionState.Reconnecting)
        {
            return;
        }

        Uri requestedHubEndpoint = new(accountContext.Environment.NotificationsBase, "/hub");
        _connection = CreateConnection(requestedHubEndpoint, accountContext);

        await _startRetry.ExecuteAsync(async token => await _connection.StartAsync(token));
    }

    public async ValueTask DisconnectAsync()
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

    public ValueTask DisposeAsync() => DisconnectAsync();

    private HubConnection CreateConnection(Uri hubEndpoint, BitwardenAccountContext accountContext)
    {
        IHubConnectionBuilder builder = new HubConnectionBuilder()
            .WithUrl(hubEndpoint, options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    SessionAccessToken sessionAccessToken = await accessTokenProvider.GetAccessTokenAsync(
                        accountContext,
                        CancellationToken.None);

                    return sessionAccessToken.ToString();
                };
            })
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions = NotificationsJsonContext.ConfiguredDefault.Options;
            })
            .WithAutomaticReconnect();

        configureConnection?.Invoke(builder);
        HubConnection connection = builder.Build();

        connection.On<NotificationEnvelope>("ReceiveMessage", DispatchAsync);

        return connection;
    }

    public Task DispatchAsync(NotificationEnvelope notificationEnvelope) =>
        notificationEnvelope.Type switch
        {
            NotificationType.SyncCipherCreate or NotificationType.SyncCipherUpdate or NotificationType.SyncCipherDelete or NotificationType.SyncLoginDelete => DispatchNotificationAsync(notificationEnvelope.Type, notificationEnvelope.Payload.Deserialize(NotificationsJsonContext.ConfiguredDefault.CipherChangedNotification)),

            NotificationType.SyncFolderCreate or NotificationType.SyncFolderUpdate or NotificationType.SyncFolderDelete => DispatchNotificationAsync(notificationEnvelope.Type, notificationEnvelope.Payload.Deserialize(NotificationsJsonContext.ConfiguredDefault.FolderChangedNotification)),

            NotificationType.SyncVault
                or NotificationType.SyncCiphers
                or NotificationType.SyncOrgKeys
                or NotificationType.SyncSettings
                or NotificationType.SyncOrganizations
                or NotificationType.SyncPolicy =>
                DispatchNotificationAsync(notificationEnvelope.Type, notificationEnvelope.Payload.Deserialize(NotificationsJsonContext.ConfiguredDefault.VaultSyncRequestedNotification)),

            NotificationType.SyncOrganizationStatusChanged =>
                DispatchNotificationAsync(notificationEnvelope.Type, notificationEnvelope.Payload.Deserialize(NotificationsJsonContext.ConfiguredDefault.OrganizationStatusPushNotification)),

            NotificationType.SyncOrganizationCollectionSettingChanged =>
                DispatchNotificationAsync(notificationEnvelope.Type, notificationEnvelope.Payload.Deserialize(NotificationsJsonContext.ConfiguredDefault.OrganizationCollectionSettingChangedPushNotification)),

            NotificationType.LogOut =>
                DispatchNotificationAsync(notificationEnvelope.Type, notificationEnvelope.Payload.Deserialize(NotificationsJsonContext.ConfiguredDefault.LogOutNotification)),

            NotificationType.SyncSendCreate or NotificationType.SyncSendUpdate or NotificationType.SyncSendDelete =>
                DispatchNotificationAsync(notificationEnvelope.Type, notificationEnvelope.Payload.Deserialize(NotificationsJsonContext.ConfiguredDefault.SyncSendNotification)),

            NotificationType.AuthRequest or NotificationType.AuthRequestResponse =>
                DispatchNotificationAsync(notificationEnvelope.Type, notificationEnvelope.Payload.Deserialize(NotificationsJsonContext.ConfiguredDefault.AuthRequestPushNotification)),

            NotificationType.OrganizationBankAccountVerified =>
                DispatchNotificationAsync(notificationEnvelope.Type, notificationEnvelope.Payload.Deserialize(NotificationsJsonContext.ConfiguredDefault.OrganizationBankAccountVerifiedPushNotification)),

            NotificationType.ProviderBankAccountVerified =>
                DispatchNotificationAsync(notificationEnvelope.Type, notificationEnvelope.Payload.Deserialize(NotificationsJsonContext.ConfiguredDefault.ProviderBankAccountVerifiedPushNotification)),

            NotificationType.AutoConfirmMember =>
                DispatchNotificationAsync(notificationEnvelope.Type, notificationEnvelope.Payload.Deserialize(NotificationsJsonContext.ConfiguredDefault.AutoConfirmMemberNotification)),

            NotificationType.PremiumStatusChanged =>
                DispatchNotificationAsync(notificationEnvelope.Type, notificationEnvelope.Payload.Deserialize(NotificationsJsonContext.ConfiguredDefault.PremiumStatusChangedNotification)),

            _ => Task.CompletedTask,
        };

    private async Task DispatchNotificationAsync<TNotification>(
        NotificationType type,
        TNotification notification,
        CancellationToken cancellationToken = default)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();

        INotificationHandler<TNotification>? handler = scope.ServiceProvider.GetService<INotificationHandler<TNotification>>();
        if (handler is not null)
        {
            await handler.HandleAsync(type, notification, cancellationToken);
        }
    }

}

