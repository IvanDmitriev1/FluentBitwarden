using BitwardenApi.Cryptography;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace BitwardenApi.Models;

public readonly record struct EncryptedUserKey(EncString Value)
{
    public static EncryptedUserKey Create(EncString value) => new(value);

    public byte[] Decrypt(ReadOnlySpan<char> masterPassword, ReadOnlySpan<char> salt, KdfConfig kdfConfig)
    {
        Span<byte> stretchedMasterKey = stackalloc byte[64];
        MasterPassword.StretchMasterKey(masterPassword, salt, kdfConfig, stretchedMasterKey);

        byte[] userKey = new byte[Value.MaxPlaintextByteCount];
        int bytesWritten = Value.DecodeTo(stretchedMasterKey, userKey);
        if (bytesWritten == userKey.Length)
            return userKey;

        try
        {
            return userKey[..bytesWritten].ToArray();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(userKey);
        }
    }
}

public readonly record struct EncryptedPrivateKey(EncString Value)
{
    public static EncryptedPrivateKey Create(EncString value) => new(value);
}
