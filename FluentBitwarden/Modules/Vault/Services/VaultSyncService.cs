using System.Linq;
using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Modules.Vault.Abstractions;
using BitwardenApi.Modules.Vault.Models;
using BitwardenApi.Modules.Vault.VaultDataParser;
using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Vault.Abstractions;
using FluentBitwarden.Modules.Vault.Models;
using FluentBitwarden.Modules.Vault.Repositories;
using FluentBitwarden.Shared.Services.Abstractions;
using System.Diagnostics;

namespace FluentBitwarden.Modules.Vault.Services;

[Fody.ConfigureAwait(false)]
internal sealed class VaultSyncService(
    IUnitOfWorkFactory unitOfWorkFactory,
    ICurrentSessionAccessor sessionAccessor,
    IVaultApiClient vaultApiClient,
    IConnectivityService connectivityService) : IVaultSyncService
{
    public event EventHandler<VaultChangedEventArgs>? VaultChanged;
    public IReadOnlyList<Cipher> Ciphers { get; private set; } = [];
    public IReadOnlyList<Folder> Folders { get; private set; } = [];

    public void LoadAllFromDb(DecryptedUserKey decryptedUserKey)
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

        var folders = unitOfWork.VaultRepository.GetAllFolders(decryptedUserKey.UserId)
            .Select(dto => VaultDataParser.ParseAndDecryptFolder(ref dto, decryptedUserKey)).ToList();

        Ciphers = ciphers;
        Folders = folders;

        OnVaultChanged(new VaultChangedEventArgs(VaultChangeKind.FullReload));
    }

    public async Task<VaultSyncResult> SyncVaultAsync(CancellationToken token)
    {
        if (!connectivityService.HasInternetAccess)
            return VaultSyncResult.SkippedOffline;

        var currentUser = sessionAccessor.CurrentUser;

        try
        {
            if (!await HasRemoteChangesAsync(currentUser, token))
                return VaultSyncResult.NoChanges;

            await using var syncPayload = await vaultApiClient.GetSyncAsync(token);

            using var unitOfWork = unitOfWorkFactory.Create();
            var repository = new VaultSyncRepository(unitOfWork.Transaction, currentUser);

            await syncPayload.ParseAsync(repository, token);
            unitOfWork.AccountRepository.UpdateSyncTime(currentUser, DateTimeOffset.UtcNow);
            unitOfWork.SaveChanges();

            return VaultSyncResult.Synced;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Vault sync failed: {ex}");
            return VaultSyncResult.Failed;
        }
    }

    private async Task<bool> HasRemoteChangesAsync(UserId currentUser, CancellationToken token)
    {
        using var unitOfWork = unitOfWorkFactory.Create();
        var lastSync = unitOfWork.AccountRepository.GetLastSyncTime(currentUser);

        var revisionDate = await vaultApiClient.GetRevisionDateAsync(token);
        return lastSync < revisionDate;
    }

    private void OnVaultChanged(VaultChangedEventArgs e)
    {
        VaultChanged?.Invoke(this, e);
    }
}
