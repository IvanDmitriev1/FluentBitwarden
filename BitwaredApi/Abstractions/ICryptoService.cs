using BitwaredApi.Models.Auth;
using BitwaredApi.Models.Vault;

namespace BitwaredApi.Abstractions;

internal interface ICryptoService
{
    MasterPasswordAuth DeriveMasterPasswordAuth(
        string email,
        string masterPassword,
        KdfConfigModel kdfConfig,
        string? kdfSalt = null);

    byte[] DecryptUserKey(EncString encryptedUserKey, ReadOnlySpan<byte> stretchedMasterKey);
    byte[] DecryptRsaWrappedKey(EncString encryptedUserKey, ReadOnlySpan<byte> privateKeyPkcs8);
    byte[] UnwrapKey(EncString encryptedKey, ReadOnlySpan<byte> wrappingKey);
    string DecryptString(EncString encryptedValue, ReadOnlySpan<byte> key);
    string CreateFingerprintPhrase(string email, ReadOnlySpan<byte> publicKey);
}
