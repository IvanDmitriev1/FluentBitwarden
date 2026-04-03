using BitwardenApi.Modules.Notifications.Abstractions;
using BitwardenApi.Modules.Notifications.Models;
using BitwardenApi.Shared.Serialization;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace BitwardenApi.Modules.Notifications.Services;

internal sealed class NotificationDispatcher(IServiceProvider serviceProvider) : INotificationDispatcher
{
    [RequiresDynamicCode("Calls BitwardenApi.Modules.Notifications.Services.NotificationDispatcher.PublishAsync<TNotification>(JsonElement, CancellationToken)")]
    [RequiresUnreferencedCode("Calls BitwardenApi.Modules.Notifications.Services.NotificationDispatcher.PublishAsync<TNotification>(JsonElement, CancellationToken)")]
    public Task DispatchAsync(in NotificationEnvelope notificationEnvelope, CancellationToken cancellationToken) =>
        notificationEnvelope.Type switch
        {
            NotificationType.SyncCipherCreate => PublishAsync<CipherChangedNotification>(notificationEnvelope.Payload,
                cancellationToken),
            NotificationType.SyncCipherUpdate => PublishAsync<CipherChangedNotification>(notificationEnvelope.Payload,
                cancellationToken),
            NotificationType.SyncCipherDelete => PublishAsync<CipherChangedNotification>(notificationEnvelope.Payload,
                cancellationToken),

            NotificationType.SyncFolderCreate => PublishAsync<FolderChangedNotification>(notificationEnvelope.Payload,
                cancellationToken),
            NotificationType.SyncFolderUpdate => PublishAsync<FolderChangedNotification>(notificationEnvelope.Payload,
                cancellationToken),
            NotificationType.SyncFolderDelete => PublishAsync<FolderChangedNotification>(notificationEnvelope.Payload,
                cancellationToken),

            NotificationType.SyncVault
                or NotificationType.SyncCiphers
                or NotificationType.SyncOrgKeys
                or NotificationType.SyncSettings
                or NotificationType.SyncOrganizations
                or NotificationType.SyncOrganizationStatusChanged
                or NotificationType.SyncOrganizationCollectionSettingChanged
                or NotificationType.SyncPolicy =>
                PublishAsync<VaultSyncRequestedNotification>(notificationEnvelope.Payload, cancellationToken),

            _ => Task.CompletedTask,
        };

    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(JsonElement, JsonSerializerOptions)")]
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(JsonElement, JsonSerializerOptions)")]
    private async Task PublishAsync<TNotification>(
        JsonElement json,
        CancellationToken cancellationToken)
    {
        var notification =
            json.Deserialize<TNotification>(BitwardenApiJsonContext.ConfiguredDefault.Options) ??
            throw new InvalidOperationException();

        var handlers = serviceProvider
            .GetServices<INotificationHandler<TNotification>>();

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(notification, cancellationToken);
        }
    }
}