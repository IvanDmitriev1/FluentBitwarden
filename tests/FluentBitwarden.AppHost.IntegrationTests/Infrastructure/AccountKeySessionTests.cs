using System.Security.Cryptography;
using System.Text;
using BitwardenApi.Vault.Cryptography;

namespace FluentBitwarden.AppHost.IntegrationTests.Infrastructure;

public sealed class AccountKeySessionTests
{
    [Fact]
    public void GetDecryptionKey_returns_user_and_replaced_organization_keys()
    {
        byte[] userKeyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        byte[] firstOrganizationKeyBytes = Enumerable.Range(65, 64).Select(static value => (byte)value).ToArray();
        byte[] replacementOrganizationKeyBytes = Enumerable.Range(129, 64).Select(static value => (byte)value).ToArray();
        using var context = new AccountKeySessionTestContext(userKeyBytes);
        OrganizationId organizationId = OrganizationId.Parse("11111111-1111-1111-1111-111111111111");

        Assert.Equal(userKeyBytes, context.Session.GetDecryptionKey(OrganizationId.Empty).Key.ToArray());
        Assert.Throws<InvalidOperationException>(() => context.Session.GetDecryptionKey(organizationId));

        context.Session.UpdateOrganizations(new Dictionary<OrganizationId, AsymmetricEncString>
        {
            [organizationId] = context.EncryptOrganizationKey(firstOrganizationKeyBytes)
        });
        Assert.Equal(firstOrganizationKeyBytes, context.Session.GetDecryptionKey(organizationId).Key.ToArray());

        context.Session.UpdateOrganizations(new Dictionary<OrganizationId, AsymmetricEncString>
        {
            [organizationId] = context.EncryptOrganizationKey(replacementOrganizationKeyBytes)
        });
        Assert.Equal(replacementOrganizationKeyBytes, context.Session.GetDecryptionKey(organizationId).Key.ToArray());

        context.Session.UpdateOrganizations(new Dictionary<OrganizationId, AsymmetricEncString>());
        Assert.Throws<InvalidOperationException>(() => context.Session.GetDecryptionKey(organizationId));
    }

    [Fact]
    public void UpdateOrganizations_rejects_empty_and_invalid_encrypted_keys()
    {
        byte[] userKeyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        using var context = new AccountKeySessionTestContext(userKeyBytes);
        OrganizationId organizationId = OrganizationId.Parse("11111111-1111-1111-1111-111111111111");

        Assert.Throws<InvalidOperationException>(() => context.Session.UpdateOrganizations(
            new Dictionary<OrganizationId, AsymmetricEncString>
            {
                [organizationId] = AsymmetricEncString.Empty
            }));

        byte[] malformedPackedKey = new byte[14];
        malformedPackedKey[0] = (byte)EncryptionType.Rsa2048_OaepSha256_B64;
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(malformedPackedKey.AsSpan(5, sizeof(int)), 1);
        var malformedKey = AsymmetricEncString.FromBytes(malformedPackedKey);
        Assert.Throws<CryptographicException>(() => context.Session.UpdateOrganizations(
            new Dictionary<OrganizationId, AsymmetricEncString>
            {
                [organizationId] = malformedKey
            }));
        Assert.Throws<InvalidOperationException>(() => context.Session.GetDecryptionKey(organizationId));
    }

    [Fact]
    public void Dispose_zeroes_owned_user_and_current_organization_keys_and_rejects_further_use()
    {
        byte[] userKeyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        byte[] organizationKeyBytes = Enumerable.Range(65, 64).Select(static value => (byte)value).ToArray();
        using var context = new AccountKeySessionTestContext(userKeyBytes);
        OrganizationId organizationId = OrganizationId.Parse("11111111-1111-1111-1111-111111111111");
        context.Session.UpdateOrganizations(new Dictionary<OrganizationId, AsymmetricEncString>
        {
            [organizationId] = context.EncryptOrganizationKey(organizationKeyBytes)
        });
        SymmetricCryptoKey userKey = context.Session.UserKey;
        SymmetricCryptoKey organizationKey = context.Session.GetDecryptionKey(organizationId);

        context.Session.Dispose();

        Assert.Throws<ObjectDisposedException>(() => userKey.Key.ToArray());
        Assert.Throws<ObjectDisposedException>(() => organizationKey.Key.ToArray());
        Assert.Throws<ObjectDisposedException>(() => context.Session.GetDecryptionKey(organizationId));
        Assert.Throws<ObjectDisposedException>(() => context.Session.UpdateOrganizations(
            new Dictionary<OrganizationId, AsymmetricEncString>()));
        Assert.Throws<ObjectDisposedException>(() => context.Session.CreateAttachmentKey(
            organizationId,
            EncString.Empty,
            EncString.Empty));
    }

    [Fact]
    public async Task CreateAttachmentKey_derives_user_and_organization_cipher_keys_from_independent_bytes()
    {
        byte[] userKeyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        byte[] organizationKeyBytes = Enumerable.Range(65, 64).Select(static value => (byte)value).ToArray();
        using var context = new AccountKeySessionTestContext(userKeyBytes);
        OrganizationId organizationId = OrganizationId.Parse("11111111-1111-1111-1111-111111111111");
        context.Session.UpdateOrganizations(new Dictionary<OrganizationId, AsymmetricEncString>
        {
            [organizationId] = context.EncryptOrganizationKey(organizationKeyBytes)
        });

        (OrganizationId Id, byte[] BaseKey)[] scenarios =
        [
            (OrganizationId.Empty, userKeyBytes),
            (organizationId, organizationKeyBytes)
        ];

        foreach ((OrganizationId id, byte[] baseKey) in scenarios)
        {
            byte[] cipherKeyBytes = Enumerable.Range(17, 64).Select(static value => (byte)value).ToArray();
            byte[] attachmentKeyBytes = Enumerable.Range(81, 64).Select(static value => (byte)value).ToArray();
            byte[] plaintext = Encoding.UTF8.GetBytes("synthetic attachment content");
            EncString protectedCipherKey = EncString.Encrypt(cipherKeyBytes, baseKey);
            EncString protectedAttachmentKey = EncString.Encrypt(attachmentKeyBytes, cipherKeyBytes);
            using AttachmentKey attachmentKey = context.Session.CreateAttachmentKey(
                id,
                protectedCipherKey,
                protectedAttachmentKey);
            byte[] encryptedFile = EncryptAttachmentFile(plaintext, attachmentKeyBytes);
            using var source = new MemoryStream(encryptedFile);
            using var destination = new MemoryStream();

            await EncFile.DecryptToAsync(attachmentKey, source, destination, TestContext.Current.CancellationToken);

            Assert.Equal(plaintext, destination.ToArray());

            if (id == organizationId)
            {
                context.Session.Dispose();
                using var retainedKeySource = new MemoryStream(encryptedFile);
                using var retainedKeyDestination = new MemoryStream();
                await EncFile.DecryptToAsync(
                    attachmentKey,
                    retainedKeySource,
                    retainedKeyDestination,
                    TestContext.Current.CancellationToken);
                Assert.Equal(plaintext, retainedKeyDestination.ToArray());
            }
        }

        static byte[] EncryptAttachmentFile(byte[] plaintext, byte[] attachmentKeyBytes)
        {
            byte[] iv = Enumerable.Range(1, 16).Select(static value => (byte)value).ToArray();
            using Aes aes = Aes.Create();
            aes.Key = attachmentKeyBytes[..32];
            byte[] ciphertext = aes.EncryptCbc(plaintext, iv, PaddingMode.PKCS7);
            byte[] macInput = new byte[iv.Length + ciphertext.Length];
            iv.CopyTo(macInput, 0);
            ciphertext.CopyTo(macInput, iv.Length);
            byte[] mac = HMACSHA256.HashData(attachmentKeyBytes.AsSpan(32, 32), macInput);
            var encryptedFile = new byte[1 + iv.Length + mac.Length + ciphertext.Length];
            encryptedFile[0] = (byte)EncryptionType.AesCbc256_HmacSha256_B64;
            iv.CopyTo(encryptedFile.AsSpan(1));
            mac.CopyTo(encryptedFile.AsSpan(1 + iv.Length));
            ciphertext.CopyTo(encryptedFile.AsSpan(1 + iv.Length + mac.Length));
            return encryptedFile;
        }
    }
}
