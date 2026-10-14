using BitwardenApi.Primitives;
using BitwardenApi.Vault.Items;
using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Internal;
using FluentBitwarden.AppHost.Modules.Vault.Persistence;
using FluentBitwarden.Contracts.Modules.Vault.Operations;
using FluentBitwarden.Platform.Infrastructure.Connectivity;

namespace FluentBitwarden.AppHost.Modules.Vault.Services;

internal sealed class VaultService(
    VaultReaderRepository vaultReaderRepository,
    VaultWriterRepository vaultWriterRepository,
    VaultSyncStateRepository vaultSyncStateRepository,
    IVaultItemsApi vaultApiClient,
    IUnitOfWork unitOfWork,
    INetworkStatus networkStatus) : IVaultService
{
    public IUnlockedVault Open(BitwardenAccountContext accountContext, IAccountKeySession keySession)
    {
        if (accountContext.UserId != keySession.UserId)
            throw new InvalidOperationException("The selected account and account key must have the same user ID.");

        keySession.UpdateOrganizations(vaultReaderRepository.LoadOrganizationKeys(accountContext.UserId));

        var data = VaultDataLoader.LoadCached(vaultReaderRepository, keySession.UserId, keySession);
        return new UnlockedVault(data);
    }

    public async Task<VaultSyncResult> SyncAsync(
        IUnlockedSessionLease lease,
        CancellationToken cancellationToken)
    {
        IUnlockedVault vault = lease.Vault;
        IAccountKeySession keySession = lease.AccountKeySession;
        BitwardenAccountContext accountContext = lease.Account.BitwardenAccountContext;

        if (lease.Account.UserId != keySession.UserId)
            return VaultSyncResult.Failed;

        try
        {
            if (!networkStatus.HasInternetAccess)
                return VaultSyncResult.SkippedOffline;

            var revisionDate = await vaultApiClient.GetRevisionDateAsync(accountContext, cancellationToken);
            var userId = lease.Account.UserId;
            var lastSync = vaultSyncStateRepository.GetServerRevisionDate(userId);
            if (lastSync is not null && lastSync.Value >= revisionDate)
                return VaultSyncResult.NoChanges;

            var response = await vaultApiClient.GetSyncAsync(accountContext, cancellationToken);
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return VaultSyncResult.SkippedOffline;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            return VaultSyncResult.Failed;
        }
    }

    public async Task<VaultCipher> SaveCipherAsync(
        IUnlockedSessionLease lease,
        VaultCipher cipher,
        CancellationToken cancellationToken)
    {
        IUnlockedVault vault = lease.Vault;
        IAccountKeySession keySession = lease.AccountKeySession;
        BitwardenAccountContext accountContext = lease.Account.BitwardenAccountContext;

        if (lease.Account.UserId != keySession.UserId)
            throw new InvalidOperationException("The unlocked account and account key must have the same user ID.");

        var request = VaultCipherRequestFactory.BuildRequest(keySession.UserKey, cipher);
        VaultCipherResponse savedDto = cipher.Id.IsEmpty
            ? await vaultApiClient.CreateCipherAsync(accountContext, request, cancellationToken)
            : await vaultApiClient.UpdateCipherAsync(accountContext, cipher.Id, request, cancellationToken);

        var savedCipher = VaultDataLoader.DecryptSavedCipher(in savedDto, keySession);

        unitOfWork.Begin();
        vaultWriterRepository.UpsertCipher(lease.Account.UserId, in savedDto);
        unitOfWork.Commit();

        vault.UpsertCipher(savedCipher);
        return savedCipher;
    }
}
