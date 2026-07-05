using BitwardenApi.Infrastructure.Cryptography;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace BitwardenApi.Identity.Contracts;

public readonly record struct ProtectedUserKey(EncString Value)
{
    public static ProtectedUserKey Create(EncString value) => new(value);

    public byte[] Decrypt(ReadOnlySpan<char> masterPassword, ReadOnlySpan<char> salt, KdfConfig kdfConfig)
    {
        using var masterKey = MasterKey.Derive(masterPassword, salt, kdfConfig);
        using var stretchedMasterKey = masterKey.Stretch();

        byte[] userKey = new byte[Value.MaxPlaintextByteCount];
        int bytesWritten = Value.DecodeTo(stretchedMasterKey.Span, userKey);
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

public readonly record struct ProtectedPrivateKey(EncString Value)
{
    public static ProtectedPrivateKey Create(EncString value) => new(value);
}
