using BitwaredApi.Models.Auth;
using BitwaredApi.Models.Vault;

namespace BitwaredApi.Abstractions;

public interface ICryptoService
{
    MasterPasswordAuth DeriveMasterPasswordAuth(
        string email,
        string masterPassword,
        KdfConfigModel kdfConfig);

    byte[] DecryptUserKey(EncString encryptedUserKey, byte[] stretchedMasterKey);

    byte[] DecryptRsaWrappedKey(EncString encryptedUserKey, byte[] privateKeyPkcs8);

    byte[]? UnwrapKey(EncString? encryptedKey, byte[] wrappingKey);

    string? DecryptString(EncString? encryptedValue, byte[] key);

    string CreateFingerprintPhrase(string email, byte[] publicKey);

    void ZeroMemory(byte[]? buffer);
}
