using BitwardenApi.Contracts;
using BitwardenApi.Internal.Serialization;
using BitwardenApi.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization.Metadata;

namespace BitwardenApi.Internal.Notifications;

internal sealed class NotificationDispatcher(IServiceProvider serviceProvider) : INotificationDispatcher
{
    public Task DispatchAsync(in NotificationEnvelope notificationEnvelope, CancellationToken cancellationToken) =>
        notificationEnvelope.Type switch
        {
            NotificationType.SyncCipherCreate => PublishAsync(notificationEnvelope.Payload,
                BitwardenApiJsonContext.ConfiguredDefault.CipherChangedNotification,
                cancellationToken),
            NotificationType.SyncCipherUpdate => PublishAsync(notificationEnvelope.Payload,
                BitwardenApiJsonContext.ConfiguredDefault.CipherChangedNotification,
                cancellationToken),
            NotificationType.SyncCipherDelete => PublishAsync(notificationEnvelope.Payload,
                BitwardenApiJsonContext.ConfiguredDefault.CipherChangedNotification,
                cancellationToken),

            NotificationType.SyncFolderCreate => PublishAsync(notificationEnvelope.Payload,
                BitwardenApiJsonContext.ConfiguredDefault.FolderChangedNotification,
                cancellationToken),
            NotificationType.SyncFolderUpdate => PublishAsync(notificationEnvelope.Payload,
                BitwardenApiJsonContext.ConfiguredDefault.FolderChangedNotification,
                cancellationToken),
            NotificationType.SyncFolderDelete => PublishAsync(notificationEnvelope.Payload,
                BitwardenApiJsonContext.ConfiguredDefault.FolderChangedNotification,
                cancellationToken),

            NotificationType.SyncVault
                or NotificationType.SyncCiphers
                or NotificationType.SyncOrgKeys
                or NotificationType.SyncSettings
                or NotificationType.SyncOrganizations
                or NotificationType.SyncOrganizationStatusChanged
                or NotificationType.SyncOrganizationCollectionSettingChanged
                or NotificationType.SyncPolicy =>
                PublishAsync(notificationEnvelope.Payload,
                    BitwardenApiJsonContext.ConfiguredDefault.VaultSyncRequestedNotification,
                    cancellationToken),

            _ => Task.CompletedTask,
        };

    private async Task PublishAsync<TNotification>(
        JsonElement json,
        JsonTypeInfo<TNotification> jsonTypeInfo,
        CancellationToken cancellationToken)
    {
        var notification =
            JsonSerializer.Deserialize(json, jsonTypeInfo) ??
            throw new InvalidOperationException();

        var handlers = serviceProvider
            .GetServices<INotificationHandler<TNotification>>();

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(notification, cancellationToken);
        }
    }
}
