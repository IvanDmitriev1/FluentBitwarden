using System.Security.Cryptography;

namespace BitwardenApi.Vault.Cryptography;

public sealed class UnlockedUserKey(UserId userId, byte[] userKey) : SymmetricCryptoKey(userKey)
{
    public UserId UserId { get; } = userId;

    public PrivateKey CreatePrivateKey(ProtectedPrivateKey protectedPrivateKey)
    {
        var protectedPrivateKeyValue = protectedPrivateKey.Value;
        Span<byte> privateKeyBuffer = new byte[protectedPrivateKeyValue.MaxPlaintextByteCount];

        try
        {
            int bytesWritten = protectedPrivateKeyValue.DecodeTo(Key, privateKeyBuffer);
            return PrivateKey.Import(privateKeyBuffer[..bytesWritten]);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKeyBuffer);
        }
    }
}
