using Windows.Networking.Connectivity;
using FluentBitwarden.AppHost.Infrastructure.Data.Abstractions;
using FluentBitwarden.AppHost.Modules.Vault.Persistence.Repositories;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using FluentBitwarden.Platform.Infrastructure;

namespace FluentBitwarden.AppHost.Modules.Vault.Workspace.Internal;

[Fody.ConfigureAwait(false)]
internal sealed class VaultSynchronizer(
    IUnitOfWorkFactory unitOfWorkFactory,
    IVaultItemsApi vaultApiClient)
{
    public async Task<VaultSyncResult> SyncAsync(
        BitwardenAccountContext accountContext,
        DecryptedUserKey decryptedUserKey,
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
        var lastSync = unitOfWork.AccountProfileRepository.GetLastSyncTime(accountContext.UserId);

        var revisionDate = await vaultApiClient.GetRevisionDateAsync(accountContext, cancellationToken);
        return lastSync < revisionDate;
    }
}
