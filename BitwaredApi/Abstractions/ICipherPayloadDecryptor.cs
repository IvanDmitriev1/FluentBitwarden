using BitwaredApi.Models.Vault;

namespace BitwaredApi.Abstractions;

public interface ICipherPayloadDecryptor
{
    VaultDecryptionOutcome<DecryptedCipher> DecryptCipher(
        CipherSyncItem item,
        Stream payload,
        byte[] userKey);
}
