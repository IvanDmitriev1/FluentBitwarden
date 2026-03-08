using BitwaredApi.Models.Vault;

namespace BitwaredApi.Abstractions;

public interface IVaultService
{
    ValueTask<SyncSummary> SyncAsync(CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<DecryptedCipher>> ListCiphersAsync(CancellationToken cancellationToken = default);

    ValueTask<DecryptedCipher?> GetCipherAsync(string id, CancellationToken cancellationToken = default);
}
