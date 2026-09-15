using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;

/// <summary>
/// Vault's sibling-facing read surface over the currently-unlocked vault. Feature modules (the SSH
/// agent, the browser extension) read decrypted ciphers through this without reaching into the
/// Vault module's internals, and without having to resolve a session first: reads against a locked
/// vault come back empty.
/// </summary>
internal interface IUnlockedVaultReader
{
    VaultCipher? GetCipher(CipherId id);

    VaultCipher[] GetCiphers(VaultCipherQuery query);
}
