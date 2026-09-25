using BitwardenApi.Vault.Cryptography;
using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.AppHost.Modules.Vault.Internal;

internal sealed class UnlockedVault(UnlockedUserKey unlockedUserKey) : IUnlockedVault
{
    public UserId UserId => unlockedUserKey.UserId;

    public VaultCipher? GetCipher(CipherId id)
    {
        return null;
    }

    public VaultCipher[] GetCiphers(VaultCipherQuery query)
    {
        return [];
    }

    public VaultFolder[] GetFolders()
    {
        return [];
    }
}
