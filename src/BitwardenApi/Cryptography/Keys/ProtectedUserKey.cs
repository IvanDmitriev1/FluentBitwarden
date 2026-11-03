using System.Security.Cryptography;

using BitwardenApi.Cryptography.Encryption;
using BitwardenApi.Cryptography.KeyDerivation;

namespace BitwardenApi.Cryptography.Keys;

public readonly record struct ProtectedUserKey(EncString Value)
{
    public static ProtectedUserKey Create(EncString value) => new(value);

    public byte[] Decrypt(ReadOnlySpan<char> masterPassword, ReadOnlySpan<char> salt, KdfConfig kdfConfig)
    {
        using var masterKey = MasterKey.Derive(masterPassword, salt, kdfConfig);

        switch (Value.CreateParts().Type)
        {
            case EncryptionType.AesCbc256_B64:
                return Value.DecodeToArray(masterKey.Span);
            case EncryptionType.AesCbc256_HmacSha256_B64:
                using (StretchedMasterKey stretchedMasterKey = masterKey.Stretch())
                    return Value.DecodeToArray(stretchedMasterKey.Span);
            default:
                throw new CryptographicException("Unsupported protected user key EncString type.");
        }
    }
}
