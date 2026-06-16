using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;

internal interface IUnlockedVaultReader
{
    VaultCipher? GetCipher(CipherId id);
    VaultCipher[] GetCiphers(VaultCipherQuery query);
    VaultFolder[] GetFolders();
    VaultCollection[] GetCollections();

}
