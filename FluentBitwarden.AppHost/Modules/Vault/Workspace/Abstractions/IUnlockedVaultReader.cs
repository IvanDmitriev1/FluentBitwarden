using FluentBitwarden.Contracts.Modules.Vault.Models;

namespace FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;

internal interface IUnlockedVaultReader
{
    bool IsOpen { get; }

    VaultCipher? GetCipher(CipherId id);
    VaultCipher[] GetCiphers(VaultCipherQuery query);
    VaultFolder[] GetFolders();
    VaultCollection[] GetCollections();

}
