using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;

/// <summary>
/// One account's decrypted vault, as an opaque handle. The Sessions module holds a reference inside
/// its session snapshot without ever seeing the contents, which is what keeps "an unlocked session
/// implies its vault" true by construction rather than by convention.
/// </summary>
/// <remarks>
/// Every handle is immutable: a mutation produces a new handle that replaces the whole reference,
/// so lock-free readers always observe a coherent vault. <see cref="UserId"/> is the only thing
/// Sessions can inspect, and exists so it can assert that a handle belongs to the account it is
/// being installed against.
/// </remarks>
internal interface IUnlockedVault
{
    /// <summary>The account this vault was decrypted for.</summary>
    UserId UserId { get; }

    VaultCipher? GetCipher(CipherId id);

    VaultCipher[] GetCiphers(VaultCipherQuery query);

    VaultFolder[] GetFolders();
}
