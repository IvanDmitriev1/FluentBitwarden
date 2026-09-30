using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.AppHost.Modules.Vault.Contracts;

public interface IUnlockedVault : IDisposable
{
    public UserId UserId { get; }

    VaultCipher? GetCipher(CipherId id);

    VaultCipher[] GetCiphers(VaultCipherQuery query);

    VaultFolder[] GetFolders();
}
