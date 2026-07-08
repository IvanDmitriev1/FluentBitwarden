using Windows.Networking.Connectivity;
using FluentBitwarden.AppHost.Modules.Vault.Persistence.Repositories;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using FluentBitwarden.Platform.Infrastructure;
using FluentBitwarden.AppHost.Data.Abstractions;
using BitwardenApi.Vault.Cryptography;

namespace FluentBitwarden.AppHost.Modules.Vault.Workspace.Internal;

[Fody.ConfigureAwait(false)]
internal sealed class VaultSynchronizer(
    IUnitOfWorkFactory unitOfWorkFactory,
    IVaultItemsApi vaultApiClient)
{
    public async Task<VaultSyncResult> SyncAsync(
        BitwardenAccountContext accountContext,
        UserKey decryptedUserKey,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        if (!NetworkInformation.HasInternetAccess)
            return VaultSyncResult.SkippedOffline;

        try
        {
            if (!force && !await HasRemoteChangesAsync(accountContext, cancellationToken))
            {
                return VaultSyncResult.NoChanges;
            }

            var response = await vaultApiClient.GetSyncAsync(accountContext, cancellationToken);
            if (response.Profile.Id != decryptedUserKey.UserId)
                throw new InvalidDataException("Sync profile user id did not match the unlocked account.");

            using var unitOfWork = unitOfWorkFactory.Create();
            var repository = new VaultWriterRepository(unitOfWork.Transaction, decryptedUserKey.UserId);

            repository.WriteOrganizations(response.Profile.Organizations);
            repository.WriteFolders(response.Folders);
            repository.WriteCollections(response.Collections);
            repository.WriteCiphers(response.VaultCiphers);

            unitOfWork.AccountProfileRepository.UpdateSyncedProfile(decryptedUserKey.UserId, response.Profile);
            unitOfWork.SaveChanges();

            return VaultSyncResult.Synced;
        }
        catch (OperationCanceledException)
        {
            return VaultSyncResult.SkippedOffline;
        }
        catch (Exception e)
        {
            UnhandledExceptionLogger.WriteException(e);
            return VaultSyncResult.Failed;
        }
    }
    private async Task<bool> HasRemoteChangesAsync(
        BitwardenAccountContext accountContext,
        CancellationToken cancellationToken)
    {
        using var unitOfWork = unitOfWorkFactory.Create();
        var lastSync = unitOfWork.VaultReaderRepository.GetLastSyncTime(accountContext.UserId);

        var revisionDate = await vaultApiClient.GetRevisionDateAsync(accountContext, cancellationToken);
        return lastSync is null || lastSync <= revisionDate;
    }
}
