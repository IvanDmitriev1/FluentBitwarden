using BitwardenApi.Primitives;
using BitwardenApi.Vault.Items;
using BitwardenApi.Vault.Items.Contracts;
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

        var data = VaultDataLoader.LoadCached(vaultReaderRepository, keySession.UserId, keySession);
        return new UnlockedVault(accountContext, data);
    }

    public async Task<VaultSyncResult> SyncAsync(
        IUnlockedVault vault,
        IAccountKeySession keySession,
        CancellationToken cancellationToken)
    {
        if (vault.AccountContext.UserId != keySession.UserId)
            return VaultSyncResult.Failed;

        try
        {
            if (!networkStatus.HasInternetAccess)
                return VaultSyncResult.SkippedOffline;

            var revisionDate = await vaultApiClient.GetRevisionDateAsync(vault.AccountContext, cancellationToken);
            var userId = vault.AccountContext.UserId;
            var lastSync = vaultSyncStateRepository.GetServerRevisionDate(userId);
            if (lastSync is not null && lastSync.Value >= revisionDate)
                return VaultSyncResult.NoChanges;

            var response = await vaultApiClient.GetSyncAsync(vault.AccountContext, cancellationToken);
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
        IUnlockedVault vault,
        IAccountKeySession keySession,
        VaultCipher cipher,
        CancellationToken cancellationToken)
    {
        if (vault.AccountContext.UserId != keySession.UserId)
            throw new InvalidOperationException("The unlocked account and account key must have the same user ID.");

        var request = VaultCipherRequestFactory.BuildRequest(keySession.UserKey, cipher);
        VaultCipherResponse savedDto = cipher.Id.IsEmpty
            ? await vaultApiClient.CreateCipherAsync(vault.AccountContext, request, cancellationToken)
            : await vaultApiClient.UpdateCipherAsync(vault.AccountContext, cipher.Id, request, cancellationToken);

        var organizationKeys = vaultReaderRepository.LoadOrganizationKeys(vault.AccountContext.UserId);
        var savedCipher = VaultDataLoader.DecryptSavedCipher(in savedDto, keySession, organizationKeys);

        unitOfWork.Begin();
        vaultWriterRepository.UpsertCipher(vault.AccountContext.UserId, in savedDto);
        unitOfWork.Commit();

        vault.UpsertCipher(savedCipher);
        return savedCipher;
    }
}
