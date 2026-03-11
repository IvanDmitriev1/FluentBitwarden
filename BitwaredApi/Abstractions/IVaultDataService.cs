using BitwaredApi.Models.Vault;

namespace BitwaredApi.Abstractions;

public interface IVaultDataService
{
    ValueTask<VaultSyncResult> SyncAsync(
        VaultSyncRequest request,
        CancellationToken cancellationToken = default);

    DecryptedCipher DecryptCipher(EncryptedCipherRecord record, byte[] userKey);

    IReadOnlyList<DecryptedCipher> DecryptCiphers(IReadOnlyList<EncryptedCipherRecord> records, byte[] userKey);
}
