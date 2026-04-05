using BitwardenApi.Modules.Identity.Models;
using FluentBitwarden.Modules.Vault.Models;

namespace FluentBitwarden.Modules.Vault.Abstractions;

public interface IVaultSyncService
{
    Task<VaultSyncResult> SyncVaultAsync();

    void Test1(DecryptedUserKey decryptedUserKey);
}
