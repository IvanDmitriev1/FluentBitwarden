using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Modules.Vault.Models;
using FluentBitwarden.Modules.Vault.Models;

namespace FluentBitwarden.Modules.Vault.Abstractions;

public interface IVaultSyncService
{
    event EventHandler<VaultChangedEventArgs> VaultChanged;

    IReadOnlyList<Cipher> Ciphers { get; }
    IReadOnlyList<Folder> Folders { get; }

    void LoadAllFromDb(DecryptedUserKey decryptedUserKey);
    Task<VaultSyncResult> SyncVaultAsync(CancellationToken token);
}
