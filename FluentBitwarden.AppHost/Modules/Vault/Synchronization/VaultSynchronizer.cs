using BitwardenApi.Contracts;
using FluentBitwarden.AppHost.Infrastructure.Data.Abstractions;
using FluentBitwarden.AppHost.Modules.Vault.Persistence.Repositories;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.Contracts.Infrastructure.Shared;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using FluentBitwarden.AppHost.Modules.Accounts.Unlock.Abstractions;

namespace FluentBitwarden.AppHost.Modules.Vault.Synchronization;

internal sealed class VaultSynchronizer(
    IUnitOfWorkFactory unitOfWorkFactory,
    IVaultApiClient vaultApiClient,
    IVaultWorkspace vaultWorkspace,
    IUnlockedAccountAccessor unlockedAccountAccessor) : IVaultSynchronizer
{
    public async ValueTask<VaultSyncResult> SyncAsync(CancellationToken cancellationToken)
    {
        if (!vaultWorkspace.IsOpen)
            throw new InvalidOperationException("The vault is not opened");

        try
        {
            if (!await HasRemoteChangesAsync(
                    vaultWorkspace.OpenedForUserId,
                    cancellationToken))
            {
                return VaultSyncResult.NoChanges;
            }

            var response = await vaultApiClient.GetSyncAsync(
                cancellationToken);

            using var unitOfWork = unitOfWorkFactory.Create();
            var repository = new VaultWriterRepository(
                unitOfWork.Transaction,
                vaultWorkspace.OpenedForUserId);

            repository.DeleteVaultData();

            repository.WriteFolders(CollectionsMarshal.AsSpan(response.Folders));
            repository.WriteCollections(CollectionsMarshal.AsSpan(response.Collections));
            repository.WriteCiphers(CollectionsMarshal.AsSpan(response.VaultCiphers));

            unitOfWork.AccountProfileRepository.UpdateSyncTime(
                vaultWorkspace.OpenedForUserId,
                DateTimeOffset.UtcNow);

            unitOfWork.SaveChanges();

            vaultWorkspace.Reload(unlockedAccountAccessor.UserKey);
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