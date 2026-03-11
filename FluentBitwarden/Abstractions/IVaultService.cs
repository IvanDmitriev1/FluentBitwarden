using BitwaredApi.Models.Auth;
using BitwaredApi.Models.Vault;
using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.Abstractions;

/// <summary>
/// Provides the public vault workflow for session adoption, unlock, sync, and cached reads.
/// </summary>
public interface IVaultService
{
    /// <summary>
     /// Gets the current vault state for the active account.
     /// </summary>
    ValueTask<VaultSessionState> GetSessionStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adopts a successful authentication result into the active vault session.
    /// </summary>
    ValueTask AdoptAuthenticationAsync(
        AuthenticationSuccess authentication,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlocks the vault with the supplied secret.
    /// </summary>
    ValueTask<VaultUnlockOutcome> UnlockAsync(
        string secret,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Locks the vault by clearing runtime secrets.
    /// </summary>
    ValueTask LockAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out the active account and clears its local vault data.
    /// </summary>
    ValueTask LogoutAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronizes vault data from Bitwarden into the local cache.
    /// </summary>
    ValueTask<VaultSyncOutcome> SyncAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads decrypted ciphers from the local cache.
    /// </summary>
    ValueTask<VaultReadOutcome<IReadOnlyList<DecryptedCipher>>> ListCiphersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a decrypted cipher from the local cache by identifier.
    /// </summary>
    ValueTask<VaultReadOutcome<DecryptedCipher?>> GetCipherAsync(
        string id,
        CancellationToken cancellationToken = default);
}
