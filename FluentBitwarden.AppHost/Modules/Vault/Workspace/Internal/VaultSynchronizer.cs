using Windows.Networking.Connectivity;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.Infrastructure.Extensions;

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
            var userId = decryptedUserKey.UserId;
            var repository = unitOfWork.VaultWriterRepository;

            repository.WriteOrganizations(userId, response.Profile.Organizations);
            repository.WriteFolders(userId, response.Folders);
            repository.WriteCollections(userId, response.Collections);
            repository.WriteCiphers(userId, response.VaultCiphers);

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
        var revisionDate = await vaultApiClient.GetRevisionDateAsync(accountContext, cancellationToken);

        using var unitOfWork = unitOfWorkFactory.Create();
        var lastSync = unitOfWork.VaultReaderRepository.GetLastSyncTime(accountContext.UserId);
        if (lastSync is null)
            return true;
        
        var lastSyncTrunc = lastSync.Value.TruncateToSeconds();
        var revisionTrunc = revisionDate.TruncateToSeconds();

        return lastSyncTrunc < revisionTrunc;
    }
}
