using Windows.Networking.Connectivity;
using FluentBitwarden.AppHost.Infrastructure.Data.Abstractions;
using FluentBitwarden.AppHost.Modules.Vault.Persistence.Repositories;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;

namespace FluentBitwarden.AppHost.Modules.Vault.Workspace.Internal;

[Fody.ConfigureAwait(false)]
internal sealed class VaultSynchronizer(
    IUnitOfWorkFactory unitOfWorkFactory,
    IVaultItemsApi vaultApiClient)
{
    public async Task<VaultSyncResult> SyncAsync(DecryptedUserKey decryptedUserKey, bool force = false, CancellationToken cancellationToken = default)
    {
        if (!NetworkInformation.HasInternetAccess)
            return VaultSyncResult.SkippedOffline;

        try
        {
            if (!force && !await HasRemoteChangesAsync(decryptedUserKey.UserId, cancellationToken))
            {
                return VaultSyncResult.NoChanges;
            }

            var response = await vaultApiClient.GetSyncAsync(cancellationToken);

            using var unitOfWork = unitOfWorkFactory.Create();
            var repository = new VaultWriterRepository(unitOfWork.Transaction, decryptedUserKey.UserId);

            var organizations = response.Profile.Organizations;
            if (organizations is { Length: > 0 })
                repository.WriteOrganizations(organizations);

            repository.WriteFolders(response.Folders);
            repository.WriteCollections(response.Collections);
            repository.WriteCiphers(response.VaultCiphers);

            unitOfWork.AccountProfileRepository.UpdateSyncTime(decryptedUserKey.UserId, DateTimeOffset.UtcNow);
            unitOfWork.SaveChanges();

            return VaultSyncResult.Synced;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            UnhandledExceptionLogger.WriteException(e);
            return VaultSyncResult.Failed;
        }
    }
    private async Task<bool> HasRemoteChangesAsync(UserId currentUser, CancellationToken token)
    {
        using var unitOfWork = unitOfWorkFactory.Create();
        var lastSync = unitOfWork.AccountProfileRepository.GetLastSyncTime(currentUser);

        var revisionDate = await vaultApiClient.GetRevisionDateAsync(token);
        return lastSync < revisionDate;
    }
}
