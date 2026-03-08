namespace FluentBitwarden.Abstractions.UnlockServices;

public interface ILocalVaultUnlocker
{
    bool IsUnlocked { get; }

    ValueTask<bool> IsInitializedAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    ValueTask InitializeAsync(
        string accountId,
        byte[] userKey,
        CancellationToken cancellationToken = default);

    ValueTask<byte[]> DecryptUserKeyAsync(
        string accountId,
        byte[] localVaultKey,
        CancellationToken cancellationToken = default);

    byte[] GetUnlockedLocalVaultKeyCopy();

    ValueTask LockAsync(CancellationToken cancellationToken = default);
    ValueTask ClearAsync(CancellationToken cancellationToken = default);
}
