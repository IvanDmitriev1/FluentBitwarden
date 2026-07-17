using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.Modules.Sessions.Abstractions;
using FluentBitwarden.AppHost.Modules.Sessions.Models;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;

/// <summary>The outcome of a sync, together with the vault to use from here on.</summary>
/// <remarks>
/// <see cref="Vault"/> is the handle that was passed in when the server had nothing new, and a
/// freshly decrypted one when it did. Returning it either way is what stops a caller from syncing
/// and then forgetting to adopt the result.
/// </remarks>
internal readonly record struct VaultSyncOutcome(VaultSyncResult Result, IUnlockedVault Vault);

/// <summary>The saved cipher as the server stored it, together with the vault it now belongs to.</summary>
internal readonly record struct VaultCipherSaveOutcome(VaultCipher Cipher, IUnlockedVault Vault);

/// <summary>
/// Stateless facade over the vault data pipeline: decrypting the local cache into a vault handle,
/// synchronizing with the server and saving ciphers. Shields consumers outside the Vault module
/// from its internal classes, and is the only place <see cref="IUnlockedVault"/> handles are made.
/// </summary>
/// <remarks>
/// Every method takes the current vault and returns the next one; none of them hold state between
/// calls. The session owns the handle, so ordering these against unlock/lock is the caller's job —
/// see <see cref="IVaultSessionManager.WithSessionAsync"/>.
/// </remarks>
internal interface IVaultWorkspace
{
    /// <summary>
    /// Decrypts the local vault for an unlocking session, syncing first when
    /// <paramref name="forceSync"/> is set or the local cache is empty.
    /// </summary>
    Task<IUnlockedVault> LoadAsync(
        BitwardenAccountContext accountContext,
        UserKey userKey,
        KeySession keys,
        bool forceSync,
        CancellationToken cancellationToken);

    /// <summary>
    /// Syncs with the server, re-decrypting the vault when the server had changes and returning
    /// <paramref name="current"/> unchanged when it did not.
    /// </summary>
    Task<VaultSyncOutcome> SyncAsync(
        BitwardenAccountContext accountContext,
        UserKey userKey,
        KeySession keys,
        IUnlockedVault current,
        CancellationToken cancellationToken);

    /// <summary>
    /// Saves a cipher and folds the server's answer into the vault, so no follow-up sync is needed.
    /// </summary>
    Task<VaultCipherSaveOutcome> SaveCipherAsync(
        BitwardenAccountContext accountContext,
        UserKey userKey,
        IUnlockedVault current,
        VaultCipher cipher,
        CancellationToken cancellationToken);
}
