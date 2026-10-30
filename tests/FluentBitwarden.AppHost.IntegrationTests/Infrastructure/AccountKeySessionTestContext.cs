using System.Buffers.Binary;
using System.Security.Cryptography;
using FluentBitwarden.AppHost.Modules.Account.Internal;

namespace FluentBitwarden.AppHost.IntegrationTests.Infrastructure;

internal sealed class AccountKeySessionTestContext : IDisposable
{
    private readonly RSA _organizationWrappingKey;

    public AccountKeySessionTestContext(byte[] userKeyBytes)
    {
        UserId = UserId.Parse(AccountTestData.FirstUserId);
        _organizationWrappingKey = RSA.Create(2048);
        byte[] privateKeyBytes = _organizationWrappingKey.ExportPkcs8PrivateKey();

        try
        {
            var userKey = new UnlockedUserKey(UserId, userKeyBytes.ToArray());
            var protectedPrivateKey = ProtectedPrivateKey.Create(EncString.Encrypt(privateKeyBytes, userKeyBytes));
            Session = new AccountKeySession(userKey, protectedPrivateKey);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKeyBytes);
        }
    }

    public UserId UserId { get; }
    public AccountKeySession Session { get; }

    public AsymmetricEncString EncryptOrganizationKey(ReadOnlySpan<byte> organizationKeyBytes)
    {
        using RSA publicKey = RSA.Create();
        publicKey.ImportParameters(_organizationWrappingKey.ExportParameters(false));
        byte[] encryptedKey = publicKey.Encrypt(organizationKeyBytes, RSAEncryptionPadding.OaepSHA256);
        var packed = new byte[13 + encryptedKey.Length];
        packed[0] = (byte)EncryptionType.Rsa2048_OaepSha256_B64;
        BinaryPrimitives.WriteInt32LittleEndian(packed.AsSpan(5, sizeof(int)), encryptedKey.Length);
        encryptedKey.CopyTo(packed.AsSpan(13));
        return AsymmetricEncString.FromBytes(packed);
    }

    public void Dispose()
    {
        Session.Dispose();
        _organizationWrappingKey.Dispose();
    }
}
