using BitwardenApi.Notifications.Abstractions;
using BitwardenApi.Notifications.Models;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;

namespace FluentBitwarden.AppHost.Modules.Vault.Services;

internal sealed class VaultSyncNotificationsHandler(
    IAppSessionAccessor sessionAccessor,
    IVaultService vaultService) :
    INotificationHandler<CipherChangedNotification>,
    INotificationHandler<FolderChangedNotification>
{
    public async Task HandleAsync(
        NotificationType type,
        CipherChangedNotification notification,
        CancellationToken cancellationToken = default)
    {
        if (type is not (
                NotificationType.SyncCipherCreate or
                NotificationType.SyncCipherUpdate or
                NotificationType.SyncCipherDelete) ||
            notification.Id.IsEmpty)
        {
            return;
        }

        if (sessionAccessor.AppSessionState is not AppSessionState.Unlocked unlocked ||
            notification.UserId != unlocked.Account.UserId)
        {
            return;
        }

        if (type == NotificationType.SyncCipherDelete)
            await vaultService.DeleteCipherAsync(notification.Id, notification.RevisionDate, cancellationToken);
        else
            await vaultService.SyncCipherAsync(notification.Id, cancellationToken);
    }

    public async Task HandleAsync(
        NotificationType type,
        FolderChangedNotification notification,
        CancellationToken cancellationToken = default)
    {
        if (type is not (
            NotificationType.SyncFolderCreate or
            NotificationType.SyncFolderUpdate or
            NotificationType.SyncFolderDelete) || notification.Id.IsEmpty)
        {
            return;
        }

        if (sessionAccessor.AppSessionState is not AppSessionState.Unlocked unlocked ||
            notification.UserId != unlocked.Account.UserId)
        {
            return;
        }

        if (type == NotificationType.SyncFolderDelete)
            await vaultService.DeleteFolderAsync(notification.Id, notification.RevisionDate, cancellationToken);
        else
            await vaultService.SyncFolderAsync(notification.Id, cancellationToken);
    }
}
