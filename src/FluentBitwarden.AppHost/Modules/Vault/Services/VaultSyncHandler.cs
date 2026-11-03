using BitwardenApi.Client;
using BitwardenApi.Notifications.Abstractions;
using BitwardenApi.Notifications.Models;
using BitwardenApi.Vault.Folders;
using BitwardenApi.Vault.Sync;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Internal;
using BitwardenApi.Vault.Serialization;
using FluentBitwarden.AppHost.Modules.Vault.Persistence;
using FluentBitwarden.Contracts.Modules.Vault.Operations;
using FluentBitwarden.Platform.Infrastructure.Connectivity;
using FluentBitwarden.Platform.Ipc.Abstractions;
using FluentBitwarden.Contracts.Modules.Vault.Events;

namespace FluentBitwarden.AppHost.Modules.Vault.Services;

internal sealed class VaultSyncHandler(
    IAppSessionLease appSessionLease,
    VaultWriterRepository vaultWriterRepository,
    VaultSyncStateRepository vaultSyncStateRepository,
    IVaultSyncApi vaultSyncApi,
    ICiphersApi ciphersApi,
    IFoldersApi foldersApi,
    IUnitOfWork unitOfWork,
    INetworkStatus networkStatus,
    IIpcEventPublisher eventPublisher) :
    IVaultSyncService,
    INotificationHandler<CipherChangedNotification>,
    INotificationHandler<FolderChangedNotification>
{
    public async Task<VaultSyncResult> SyncAsync()
    {
        (AccountProfile accountProfile, IUnlockedVault vault, IAccountKeySession keySession, _) = appSessionLease.RequireUnlockedState();
        BitwardenAccountContext accountContext = accountProfile.BitwardenAccountContext;

        try
        {
            if (!networkStatus.HasInternetAccess)
                return VaultSyncResult.SkippedOffline;

            var revisionDate = await vaultSyncApi.GetRevisionDateAsync(accountContext);
            var userId = accountProfile.UserId;
            var lastSync = vaultSyncStateRepository.GetServerRevisionDate(userId);
            if (lastSync is not null && lastSync.Value >= revisionDate)
                return VaultSyncResult.NoChanges;

            var response = await vaultSyncApi.GetSyncAsync(accountContext);
            if (response.Profile.Id != userId)
                throw new InvalidDataException("Sync profile user id did not match the unlocked account.");

            var data = VaultDataLoader.DecryptResponse(response, keySession);

            unitOfWork.Begin();
            vaultWriterRepository.WriteOrganizations(userId, response.Profile.Organizations);
            vaultWriterRepository.WriteFolders(userId, response.Folders);
            vaultWriterRepository.WriteCollections(userId, response.Collections);
            vaultWriterRepository.WriteCiphers(userId, response.VaultCiphers);
            vaultSyncStateRepository.UpsertServerRevisionDate(userId, revisionDate);
            unitOfWork.Commit();

            vault.ReplaceContents(data.Ciphers, data.Folders, data.Collections);
            return VaultSyncResult.Synced;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            return VaultSyncResult.Failed;
        }
    }

    public async Task HandleAsync(
        NotificationType type,
        CipherChangedNotification notification,
        CancellationToken cancellationToken = default)
    {
        if (type is not (
                NotificationType.SyncCipherCreate or
                NotificationType.SyncCipherUpdate or
                NotificationType.SyncCipherDelete or
                NotificationType.SyncLoginDelete) ||
            notification.Id.IsEmpty ||
            appSessionLease.AppSessionState is not AppSessionState.Unlocked unlocked ||
            notification.UserId != unlocked.Account.UserId)
        {
            return;
        }
        AccountProfile account = unlocked.Account;
        IUnlockedVault vault = unlocked.Vault;
        var userId = account.UserId;
        bool deleting = type is NotificationType.SyncCipherDelete or NotificationType.SyncLoginDelete;

        if (deleting)
        {
            var currentCipher = vault.GetCipher(notification.Id);
            if (currentCipher is not null && currentCipher.RevisionDate > notification.RevisionDate)
                return;

            unitOfWork.Begin();
            vaultWriterRepository.DeleteCipher(userId, notification.Id);
            vaultSyncStateRepository.UpsertServerRevisionDate(userId, notification.RevisionDate);
            unitOfWork.Commit();

            vault.RemoveCipher(notification.Id);
            await eventPublisher.PublishAsync(new CipherDeletedEvent(userId, notification.Id), cancellationToken);
            return;
        }

        var response = await ciphersApi.GetCipherAsync(account.BitwardenAccountContext, notification.Id, cancellationToken);
        var fetchedCipher = vault.GetCipher(notification.Id);

        if (fetchedCipher is not null && fetchedCipher.RevisionDate >= response.RevisionDate)
            return;

        var cipher = VaultDataLoader.DecryptSavedCipher(in response, unlocked.AccountKeySession);
        unitOfWork.Begin();
        vaultWriterRepository.UpsertCipher(userId, in response);
        vaultSyncStateRepository.UpsertServerRevisionDate(userId, response.RevisionDate);
        unitOfWork.Commit();

        vault.UpsertCipher(cipher);
        await eventPublisher.PublishAsync(new CipherChangedEvent(userId, cipher), cancellationToken);
    }

    public async Task HandleAsync(
        NotificationType type,
        FolderChangedNotification notification,
        CancellationToken cancellationToken = default)
    {
        if (type is not (
                NotificationType.SyncFolderCreate or
                NotificationType.SyncFolderUpdate or
                NotificationType.SyncFolderDelete) ||
            notification.Id.IsEmpty ||
            appSessionLease.AppSessionState is not AppSessionState.Unlocked unlocked ||
            notification.UserId != unlocked.Account.UserId)
        {
            return;
        }

        AccountProfile account = unlocked.Account;
        IUnlockedVault vault = unlocked.Vault;
        var userId = account.UserId;
        if (type is NotificationType.SyncFolderDelete)
        {
            var currentFolder = vault.GetFolder(notification.Id);
            if (currentFolder is not null && currentFolder.RevisionDate > notification.RevisionDate)
                return;

            unitOfWork.Begin();
            vaultWriterRepository.DeleteFolder(userId, notification.Id);
            vaultSyncStateRepository.UpsertServerRevisionDate(userId, notification.RevisionDate);
            unitOfWork.Commit();

            vault.RemoveFolder(notification.Id);
            await eventPublisher.PublishAsync(new FolderDeletedEvent(userId, notification.Id), cancellationToken);
            return;
        }

        var response = await foldersApi.GetFolderAsync(account.BitwardenAccountContext, notification.Id, cancellationToken);
        var fetchedFolder = vault.GetFolder(notification.Id);

        if (fetchedFolder is not null && fetchedFolder.RevisionDate >= response.RevisionDate)
            return;

        var folder = VaultMetadataReader.ReadFolder(ref response, unlocked.AccountKeySession.UserKey);
        unitOfWork.Begin();
        vaultWriterRepository.UpdateFolder(userId, in response);
        vaultSyncStateRepository.UpsertServerRevisionDate(userId, response.RevisionDate);
        unitOfWork.Commit();
        vault.UpsertFolder(folder);
        await eventPublisher.PublishAsync(new FolderChangedEvent(userId, folder), cancellationToken);
    }
}
