using BitwardenApi.Client;
using BitwardenApi.Vault.Folders;
using BitwardenApi.Vault.Sync;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Internal;
using FluentBitwarden.AppHost.Modules.Vault.Persistence;
using FluentBitwarden.AppHost.Modules.Vault.Persistence.Parsing;
using FluentBitwarden.Contracts.Modules.Vault.Events;
using FluentBitwarden.Contracts.Modules.Vault.Operations;
using FluentBitwarden.Platform.Infrastructure.Connectivity;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.Modules.Vault.Services;

internal sealed class VaultService(
    IAppSessionAccessor appSessionAccessor,
    VaultReaderRepository vaultReaderRepository,
    VaultWriterRepository vaultWriterRepository,
    VaultSyncStateRepository vaultSyncStateRepository,
    IVaultSyncApi vaultSyncApi,
    ICiphersApi ciphersApi,
    IFoldersApi foldersApi,
    IUnitOfWork unitOfWork,
    INetworkStatus networkStatus,
    IIpcEventPublisher eventPublisher) : IVaultService
{
    public IUnlockedVault Open(BitwardenAccountContext accountContext, IAccountKeySession keySession)
    {
        if (accountContext.UserId != keySession.UserId)
            throw new InvalidOperationException("The selected account and account key must have the same user ID.");

        keySession.UpdateOrganizations(vaultReaderRepository.LoadOrganizationKeys(accountContext.UserId));

        var data = VaultDataLoader.LoadCached(vaultReaderRepository, keySession.UserId, keySession);
        return new UnlockedVault(data);
    }

    public async Task<VaultSyncResult> SyncAsync()
    {
        (AccountProfile accountProfile, IUnlockedVault vault, IAccountKeySession keySession, _) = appSessionAccessor.RequireUnlockedState();
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

    public async Task SyncCipherAsync(CipherId id, CancellationToken cancellationToken)
    {
        (AccountProfile accountProfile, IUnlockedVault vault, IAccountKeySession keySession, _) = appSessionAccessor.RequireUnlockedState();

        var response = await ciphersApi.GetCipherAsync(accountProfile.BitwardenAccountContext, id, cancellationToken);
        var current = vault.GetCipher(response.Id);

        if (current is not null && current.RevisionDate >= response.RevisionDate)
        {
            return;
        }

        Cipher updated = VaultDataLoader.DecryptSavedCipher(in response, keySession);
        
        unitOfWork.Begin();
        vaultWriterRepository.UpsertCipher(accountProfile.UserId, in response);
        vaultSyncStateRepository.UpsertServerRevisionDate(accountProfile.UserId, updated.RevisionDate);
        unitOfWork.Commit();

        vault.UpsertCipher(updated);
        await eventPublisher.PublishAsync(new CipherChangedEvent(accountProfile.UserId, updated), cancellationToken);
    }

    public async Task SyncFolderAsync(FolderId id, CancellationToken cancellationToken)
    {
        (AccountProfile accountProfile, IUnlockedVault vault, IAccountKeySession keySession, _) = appSessionAccessor.RequireUnlockedState();

        var response = await foldersApi.GetFolderAsync(accountProfile.BitwardenAccountContext, id, cancellationToken);
        var current = vault.GetFolder(response.Id);

        if (current is not null && current.RevisionDate >= response.RevisionDate)
            return;

        var updated = VaultDataParser.ParseAndDecryptFolder(ref response, keySession.UserKey);

        unitOfWork.Begin();
        vaultWriterRepository.UpdateFolder(accountProfile.UserId, ref response);
        vaultSyncStateRepository.UpsertServerRevisionDate(accountProfile.UserId, updated.RevisionDate);
        unitOfWork.Commit();

        vault.UpsertFolder(updated);
        await eventPublisher.PublishAsync(new FolderChangedEvent(accountProfile.UserId, updated), cancellationToken);
    }

    public async Task DeleteCipherAsync(CipherId id, DateTimeOffset revisionDate, CancellationToken cancellationToken)
    {
        (AccountProfile account, IUnlockedVault vault, _, _) = appSessionAccessor.RequireUnlockedState();
        if (vault.GetCipher(id) is { } current && current.RevisionDate > revisionDate)
            return;

        unitOfWork.Begin();
        vaultWriterRepository.DeleteCipher(account.UserId, id);
        vaultSyncStateRepository.UpsertServerRevisionDate(account.UserId, revisionDate);
        unitOfWork.Commit();

        vault.RemoveCipher(id);
        await eventPublisher.PublishAsync(new CipherDeletedEvent(account.UserId, id), cancellationToken);
    }

    public async Task DeleteFolderAsync(FolderId id, DateTimeOffset revisionDate, CancellationToken cancellationToken)
    {
        (AccountProfile account, IUnlockedVault vault, _, _) = appSessionAccessor.RequireUnlockedState();
        if (vault.GetFolder(id) is { } current && current.RevisionDate > revisionDate)
            return;

        unitOfWork.Begin();
        vaultWriterRepository.DeleteFolder(account.UserId, id);
        vaultSyncStateRepository.UpsertServerRevisionDate(account.UserId, revisionDate);
        unitOfWork.Commit();

        vault.RemoveFolder(id);
    }

    public async Task<Cipher> SaveCipherAsync(
        Cipher cipher,
        CancellationToken cancellationToken)
    {
        var (accountProfile, vault, keySession, _) = appSessionAccessor.RequireUnlockedState();
        BitwardenAccountContext accountContext = accountProfile.BitwardenAccountContext;

        var request = CipherRequestFactory.BuildRequest(keySession.UserKey, cipher);
        CipherResponse savedDto = cipher.Id.IsEmpty
            ? await ciphersApi.CreateCipherAsync(accountContext, request, cancellationToken)
            : await ciphersApi.UpdateCipherAsync(accountContext, cipher.Id, request, cancellationToken);

        var savedCipher = VaultDataLoader.DecryptSavedCipher(in savedDto, keySession);

        unitOfWork.Begin();
        vaultWriterRepository.UpsertCipher(accountProfile.UserId, in savedDto);
        unitOfWork.Commit();

        vault.UpsertCipher(savedCipher);
        return savedCipher;
    }
}
