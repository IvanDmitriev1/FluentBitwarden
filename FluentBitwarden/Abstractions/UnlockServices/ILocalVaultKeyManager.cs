namespace FluentBitwarden.Abstractions.UnlockServices;

/// <summary>
/// Manages the local vault key and encrypted user-key payload used for offline unlock.
/// </summary>
internal interface ILocalVaultKeyManager
{
    /// <summary>
    /// Indicates whether the local vault key is currently loaded in memory.
    /// </summary>
    bool IsUnlocked { get; }

    /// <summary>
    /// Checks whether local vault data exists for an account.
    /// </summary>
    ValueTask<bool> IsInitializedAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates local vault state for an account from a decrypted user key.
    /// </summary>
    ValueTask InitializeAsync(
        string accountId,
        byte[] userKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts the persisted user key with the supplied local vault key.
    /// </summary>
    ValueTask<byte[]> DecryptUserKeyAsync(
        string accountId,
        byte[] localVaultKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a copy of the unlocked local vault key.
    /// </summary>
    byte[] GetUnlockedLocalVaultKeyCopy();

    /// <summary>
    /// Clears the in-memory local vault key.
    /// </summary>
    ValueTask LockAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the local vault key and persisted local unlock state.
    /// </summary>
    ValueTask ClearAsync(CancellationToken cancellationToken = default);
}
