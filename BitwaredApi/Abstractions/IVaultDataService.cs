using BitwaredApi.Models.Vault;

namespace BitwaredApi.Abstractions;

public interface IVaultDataService
{
    ValueTask<VaultSyncResult> SyncAsync(
        VaultSyncRequest request,
        CancellationToken cancellationToken = default);

    DecryptedCipher DecryptCipher(CipherSyncItem record, byte[] userKey);

    IReadOnlyList<DecryptedCipher> DecryptCiphers(IReadOnlyList<CipherSyncItem> records, byte[] userKey);
}
