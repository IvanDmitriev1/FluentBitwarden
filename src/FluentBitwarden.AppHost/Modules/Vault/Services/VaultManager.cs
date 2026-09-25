using BitwardenApi.Vault.Cryptography;
using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Internal;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;

namespace FluentBitwarden.AppHost.Modules.Vault.Services;

internal sealed class VaultManager : IVaultManager
{
    public IUnlockedVault Open(AccountProfile account, UnlockedUserKey userKey)
    {
        return new UnlockedVault(userKey);
    }

    public Task<VaultSyncResult> Sync(IUnlockedVault vault)
    {
        return Task.FromResult(VaultSyncResult.NoChanges);
    }

    public Task SaveCipher(IUnlockedVault vault, VaultCipher cipher)
    {
        return Task.CompletedTask;
    }
}
