using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Modules.Vault.Abstractions;
using BitwardenApi.Modules.Vault.Models;
using BitwardenApi.Modules.Vault.VaultDataParser;
using BitwardenApi.Shared.Exceptions;
using FluentBitwarden.Data;
using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Modules.Connectivity.Abstractions;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Vault.Abstractions;
using FluentBitwarden.Modules.Vault.Models;
using FluentBitwarden.Modules.Vault.Repositories;
using System.Net.Http;

namespace FluentBitwarden.Modules.Vault.Services;

[Fody.ConfigureAwait(false)]
internal sealed class VaultSyncService(
    IUnitOfWorkFactory unitOfWorkFactory,
    ICurrentSessionAccessor sessionAccessor,
    IVaultApiClient vaultApiClient,
    IConnectivityService connectivityService) : IVaultSyncService
{
    public async Task<VaultSyncResult> SyncVaultAsync()
    {
        if (!connectivityService.HasInternetAccess)
            return VaultSyncResult.SkippedOffline;

        var currentUser = sessionAccessor.CurrentUser;
        using var unitOfWork = unitOfWorkFactory.Create();

        try
        {
            if (!await HasRemoteChangesAsync(unitOfWork, currentUser))
                return VaultSyncResult.NoChanges;

            await using var syncPayload = await vaultApiClient.GetSyncAsync(sessionAccessor.CurrentContext.Environment);

            var repository = new VaultSyncRepository(unitOfWork.Transaction, currentUser);
            await syncPayload.ParseAsync(repository);
            unitOfWork.AccountRepository.UpdateSyncTime(currentUser, DateTimeOffset.UtcNow);

            unitOfWork.SaveChanges();
            return VaultSyncResult.Synced;
        }
        catch (Exception ex) when (IsRecoverableFailure(ex))
        {
            return VaultSyncResult.Failed;
        }
    }

    public void Test1(DecryptedUserKey decryptedUserKey)
    {
        using var unitOfWork = unitOfWorkFactory.Create();

        List<Cipher> ciphers = [];

        unitOfWork.VaultRepository.ReadAllCiphers(
            decryptedUserKey.UserId,
            (ciphers, decryptedUserKey),
            static (state, ref readonly dto, payload) =>
            {
                var (ciphers, userKey) = state;
                ciphers.Add(VaultDataParser.ParseAndDecryptCipher(in dto, payload, userKey));
            });


    }

    private async Task<bool> HasRemoteChangesAsync(UnitOfWork unitOfWork, UserId currentUser)
    {
        var lastSync = unitOfWork.AccountRepository.GetLastSyncTime(currentUser);

        var revisionDate = await vaultApiClient.GetRevisionDateAsync(
            sessionAccessor.CurrentContext.Environment);

        return lastSync < revisionDate;
    }

    private static bool IsRecoverableFailure(Exception exception) =>
        exception is HttpRequestException
            or TaskCanceledException
            or BitwardenApiException
            or InvalidDataException
            or IOException;
}
