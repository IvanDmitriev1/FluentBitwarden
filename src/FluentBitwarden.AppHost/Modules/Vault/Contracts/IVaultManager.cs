using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;

namespace FluentBitwarden.AppHost.Modules.Vault.Contracts;

public interface IVaultManager
{
    IUnlockedVault Open(IAccountKeySession accountKeySession);

    Task<VaultSyncResult> Sync(IUnlockedVault vault);
    Task SaveCipher(IUnlockedVault vault, VaultCipher cipher);
}
