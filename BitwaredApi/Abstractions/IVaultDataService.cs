using BitwaredApi.Models.Vault;

namespace BitwaredApi.Abstractions;

public interface IVaultDataService
{
    ValueTask<VaultSyncResult> SyncAsync(
        VaultSyncRequest request,
        CancellationToken cancellationToken = default);

    VaultDecryptionOutcome<DecryptedCipher> DecryptCipher(CipherSyncItem record, byte[] userKey);

    VaultDecryptionOutcome<IReadOnlyList<DecryptedCipher>> DecryptCiphers(
        IReadOnlyList<CipherSyncItem> records,
        byte[] userKey);
}
