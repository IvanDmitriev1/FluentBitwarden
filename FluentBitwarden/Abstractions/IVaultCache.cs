using BitwaredApi.Models.Vault;

namespace FluentBitwarden.Abstractions;

/// <summary>
/// Stores and retrieves the encrypted local vault snapshot for offline access.
/// </summary>
internal interface IVaultCache
{
    /// <summary>
    /// Ensures the local vault cache is ready for use.
    /// </summary>
    ValueTask InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists an encrypted sync snapshot to the local cache.
    /// </summary>
    ValueTask SaveSyncAsync(EncryptedSyncSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists cached encrypted ciphers for an account.
    /// </summary>
    ValueTask<IReadOnlyList<EncryptedCipherRecord>> ListCiphersAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a cached encrypted cipher for an account by identifier.
    /// </summary>
    ValueTask<EncryptedCipherRecord?> GetCipherAsync(
        string accountId,
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cached sync metadata for an account.
    /// </summary>
    ValueTask<VaultSyncStateRecord?> GetSyncStateAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes cached vault data for an account.
    /// </summary>
    ValueTask ClearAccountAsync(string accountId, CancellationToken cancellationToken = default);
}
