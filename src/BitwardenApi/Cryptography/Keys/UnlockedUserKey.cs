using System.Security.Cryptography;

using BitwardenApi.Cryptography.Encryption;

namespace BitwardenApi.Cryptography.Keys;

public sealed class UnlockedUserKey(UserId userId, byte[] userKey) : SymmetricCryptoKey(userKey)
{
    public UserId UserId { get; } = userId;

    public RsaPrivateKey CreatePrivateKey(ProtectedPrivateKey protectedPrivateKey)
    {
        var protectedPrivateKeyValue = protectedPrivateKey.Value;
        Span<byte> privateKeyBuffer = new byte[protectedPrivateKeyValue.MaxPlaintextByteCount];

        try
        {
            int bytesWritten = protectedPrivateKeyValue.DecodeTo(Key, privateKeyBuffer);
            return RsaPrivateKey.Import(privateKeyBuffer[..bytesWritten]);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKeyBuffer);
        }
    }
}
