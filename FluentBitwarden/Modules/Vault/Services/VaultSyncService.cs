using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Modules.Vault.Abstractions;
using FluentBitwarden.Data;
using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Vault.Abstractions;
using FluentBitwarden.Modules.Vault.Repositories;

namespace FluentBitwarden.Modules.Vault.Services;

[Fody.ConfigureAwait(false)]
internal sealed class VaultSyncService(
    IUnitOfWorkFactory unitOfWorkFactory,
    ICurrentSessionAccessor sessionAccessor,
    IVaultApiClient vaultApiClient) : IVaultSyncService
{
    public async Task<bool> SyncVaultAsync()
    {
        var currentUser = sessionAccessor.CurrentUser;
        using var unitOfWork = unitOfWorkFactory.Create();

        if (!await GetSyncStatus(unitOfWork, currentUser))
            return false;

        await using var syncPayload = await vaultApiClient.GetSyncAsync(sessionAccessor.CurrentContext.Environment);

        var repository = new VaultSyncResponceRepository(unitOfWork.Transaction, currentUser);
        repository.DeleteVaultData(currentUser);

        await syncPayload.ParseAsync(repository);
        unitOfWork.AccountRepository.UpdateSyncTime(currentUser, DateTimeOffset.UtcNow);

        unitOfWork.SaveChanges();
        return true;
    }

    private async Task<bool> GetSyncStatus(UnitOfWork unitOfWork, UserId currentUser)
    {
        var lastSync = unitOfWork.AccountRepository.GetLastSyncTime(currentUser);

        var revisionDate = await vaultApiClient.GetRevisionDateAsync(
            sessionAccessor.CurrentContext.Environment);

        return lastSync < revisionDate;
    }
}