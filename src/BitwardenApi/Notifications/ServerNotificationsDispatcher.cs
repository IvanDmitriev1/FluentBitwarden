using BitwardenApi.Notifications.Abstractions;
using BitwardenApi.Notifications.Internal;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;

namespace BitwardenApi.Notifications;

internal sealed class ServerNotificationsDispatcher
    : IServerNotificationsDispatcher
{
    public ServerNotificationsDispatcher(
        HubConnection hubConnection,
        IServiceScopeFactory serviceScopeFactory,
        TimeProvider timeProvider)
    {
        _hubConnection = hubConnection;
        _serviceScopeFactory = serviceScopeFactory;

        hubConnection.On<NotificationEnvelope>("ReceiveMessage", DispatchAsync);

        ResiliencePipeline resiliencePipeline = new ResiliencePipelineBuilder
            {
                TimeProvider = timeProvider,
            }
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = int.MaxValue,
                Delay = TimeSpan.FromSeconds(30),
                MaxDelay = TimeSpan.FromMinutes(5),
                BackoffType = DelayBackoffType.Exponential,
            })
            .Build();

        _connectionTask =
            resiliencePipeline
                .ExecuteAsync(static async (c, token) => await c.StartAsync(token), hubConnection, _cts.Token).AsTask();
    }

    private readonly CancellationTokenSource _cts = new();
    private readonly HubConnection _hubConnection;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly Task _connectionTask;
    private bool _disposed;

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        try
        {
            _cts.Cancel();
            await _connectionTask;
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested)
        {
            // Expected during disposal.
        }
        finally
        {
            await _hubConnection.DisposeAsync();
            _cts.Dispose();
            _disposed = true;
        }
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
        await using var scope = _serviceScopeFactory.CreateAsyncScope();

        INotificationHandler<TNotification>? handler = scope.ServiceProvider.GetService<INotificationHandler<TNotification>>();
        if (handler is not null)
        {
            await handler.HandleAsync(type, notification, cancellationToken);
        }
    }

}

