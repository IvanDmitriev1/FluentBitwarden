using BitwaredApi.Models.Vault;

namespace FluentBitwarden.Abstractions;

public interface IVaultCache
{
    ValueTask InitializeAsync(CancellationToken cancellationToken = default);

    ValueTask SaveSyncAsync(EncryptedSyncSnapshot snapshot, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<EncryptedCipherRecord>> ListCiphersAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    ValueTask<EncryptedCipherRecord?> GetCipherAsync(
        string accountId,
        string id,
        CancellationToken cancellationToken = default);

    ValueTask<VaultSyncStateRecord?> GetSyncStateAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    ValueTask ClearAccountAsync(string accountId, CancellationToken cancellationToken = default);
}
