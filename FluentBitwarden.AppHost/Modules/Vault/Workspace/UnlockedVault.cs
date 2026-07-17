using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Models;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.AppHost.Modules.Vault.Workspace;

/// <summary>
/// The one implementation of <see cref="IUnlockedVault"/>, created only by <see cref="VaultWorkspace"/>.
/// Immutable: <see cref="With"/> returns a new handle instead of mutating, so a reader that has
/// resolved a handle keeps a coherent view of the vault for as long as it holds it, even while a
/// sync or a save replaces the session's current handle underneath.
/// </summary>
internal sealed class UnlockedVault(UserId userId, LoadedVaultData data) : IUnlockedVault
{
    public UserId UserId => userId;

    /// <summary>No ciphers — the local cache has never been synced for this account.</summary>
    public bool IsEmpty => data.CiphersById.Count == 0;

    /// <summary>
    /// Folds a saved cipher into a new handle, so saving one cipher costs a dictionary copy rather
    /// than a full re-decrypt of the vault.
    /// </summary>
    public UnlockedVault With(VaultCipher savedCipher)
    {
        var ciphersById = new Dictionary<CipherId, VaultCipher>(data.CiphersById)
        {
            [savedCipher.Id] = savedCipher,
        };

        return new UnlockedVault(userId, data with { CiphersById = ciphersById });
    }

    public VaultCipher? GetCipher(CipherId id) => data.CiphersById.GetValueOrDefault(id);

    public VaultCipher[] GetCiphers(VaultCipherQuery query) => data.FilterCiphers(query);

    public VaultFolder[] GetFolders() => data.Folders.ToArray();
}
