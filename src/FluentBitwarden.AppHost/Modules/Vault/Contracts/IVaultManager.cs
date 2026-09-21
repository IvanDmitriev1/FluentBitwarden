using BitwardenApi.Vault.Cryptography;
using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;

namespace FluentBitwarden.AppHost.Modules.Vault.Contracts;

public interface IVaultManager
{
    IUnlockedVault Open(AccountProfile account, UnlockedUserKey userKey);

    Task<VaultSyncResult> Sync(IUnlockedVault vault);
    Task SaveCipher(IUnlockedVault vault, VaultCipher cipher);
}
