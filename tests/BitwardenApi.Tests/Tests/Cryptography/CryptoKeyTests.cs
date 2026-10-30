using System.Security.Cryptography;
using BitwardenApi.Tests.Infrastructure;

namespace BitwardenApi.Tests.Tests.Cryptography;

public sealed class CryptoKeyTests
{
    [Fact]
    public void Unlocked_user_key_owns_and_zeros_its_input_buffer()
    {
        byte[] ownedKey = CryptoTestFixtures.CombinedKey();
        var userKey = new UnlockedUserKey(UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), ownedKey);
        ReadOnlySpan<byte> borrowedKey = userKey.Key;

        userKey.Dispose();
        userKey.Dispose();

        Assert.Equal(new byte[64], borrowedKey.ToArray());
        Assert.Throws<ObjectDisposedException>(() => _ = userKey.Key.Length);
    }

    [Fact]
    public void Cipher_key_without_cipher_specific_key_copies_and_zeros_the_base_key()
    {
        using var baseKey = new UnlockedUserKey(
            UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            CryptoTestFixtures.CombinedKey());
        CipherKey cipherKey = CipherKey.Create(EncString.Empty, baseKey);
        ReadOnlyMemory<byte> borrowedKey = cipherKey.BorrowKeyMemory();
        byte[] encodedWireValue = System.Text.Encoding.UTF8.GetBytes(CryptoTestFixtures.AuthenticatedEncString);

        Assert.Equal(baseKey.Key.ToArray(), borrowedKey.ToArray());
        int decodedLength = encodedWireValue.AsSpan().DecodeEncStringInPlace(cipherKey);
        Assert.Equal(
            System.Text.Encoding.UTF8.GetBytes("fixture plaintext"),
            encodedWireValue.AsSpan(0, decodedLength).ToArray());
        baseKey.Dispose();
        Assert.Equal(64, borrowedKey.Length);
        Assert.Equal(CryptoTestFixtures.CombinedKey(), borrowedKey.ToArray());
        cipherKey.Dispose();
        Assert.Equal(new byte[64], borrowedKey.ToArray());
    }

    [Fact]
    public void Cipher_key_decrypts_a_wrapped_key_and_is_used_by_cipher_string_extensions()
    {
        using var baseKey = new UnlockedUserKey(
            UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            CryptoTestFixtures.CombinedKey());
        byte[] individualKey = [.. Enumerable.Range(32, 64).Select(static value => (byte)value)];
        EncString encryptedKey = EncString.Encrypt(individualKey, baseKey.Key);
        CipherKey cipherKey = CipherKey.Create(encryptedKey, baseKey);
        byte[] protectedPlaintext = System.Text.Encoding.UTF8.GetBytes("cipher-specific value");
        EncString encryptedValue = EncString.Encrypt(protectedPlaintext, cipherKey.Key);
        EncString encryptedBaseValue = EncString.Encrypt("base-key value", baseKey.Key);
        byte[] encodedWireValue = System.Text.Encoding.UTF8.GetBytes(CryptoTestFixtures.AuthenticatedEncString);
        int decodedLength = encodedWireValue.AsSpan().DecodeEncStringInPlace(baseKey.Key);

        try
        {
            Assert.Equal(individualKey, cipherKey.BorrowKeyMemory().ToArray());
            Assert.Equal("cipher-specific value", encryptedValue.Decode(cipherKey));
            Assert.Equal("base-key value", encryptedBaseValue.Decode(baseKey));
            Assert.Equal(
                System.Text.Encoding.UTF8.GetBytes("fixture plaintext"),
                encodedWireValue.AsSpan(0, decodedLength).ToArray());
        }
        finally
        {
            cipherKey.Dispose();
            CryptographicOperations.ZeroMemory(individualKey);
        }
    }

    [Fact]
    public void Cipher_key_creation_rejects_a_tampered_wrapped_key()
    {
        using var baseKey = new UnlockedUserKey(
            UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            CryptoTestFixtures.CombinedKey());
        EncString tampered = TestApiSupport.ParseEncString(
            CryptoTestFixtures.AuthenticatedEncString[..^1] + "A");

        Assert.Throws<CryptographicException>(() => CreateCipherKey(tampered, baseKey));
    }

    [Fact]
    public void Attachment_key_exposes_key_halves_and_zeros_them_on_disposal()
    {
        using var baseKey = new UnlockedUserKey(
            UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            CryptoTestFixtures.CombinedKey());
        CipherKey cipherKey = CipherKey.Create(EncString.Empty, baseKey);
        byte[] expectedKey = [.. Enumerable.Range(64, 64).Select(static value => (byte)value)];
        EncString encryptedKey = EncString.Encrypt(expectedKey, cipherKey.Key);
        AttachmentKey attachmentKey = AttachmentKey.Create(encryptedKey, cipherKey);
        ReadOnlySpan<byte> borrowedKey = attachmentKey.Key;

        try
        {
            Assert.Equal(expectedKey, attachmentKey.Key.ToArray());
            Assert.Equal(expectedKey[..32], attachmentKey.AesKey.ToArray());
            Assert.Equal(expectedKey[32..], attachmentKey.MacKey.ToArray());
            Assert.Equal(32, attachmentKey.AesKey.Length);
            Assert.Equal(32, attachmentKey.MacKey.Length);
        }
        finally
        {
            attachmentKey.Dispose();
            attachmentKey.Dispose();
            cipherKey.Dispose();
            CryptographicOperations.ZeroMemory(expectedKey);
        }

        Assert.Equal(new byte[64], borrowedKey.ToArray());
        Assert.Throws<ObjectDisposedException>(() => _ = attachmentKey.Key.Length);
        Assert.Throws<ObjectDisposedException>(() => _ = attachmentKey.AesKey.Length);
        Assert.Throws<ObjectDisposedException>(() => _ = attachmentKey.MacKey.Length);
    }

    [Fact]
    public void Protected_user_key_decrypts_after_master_key_derivation_and_stretching()
    {
        const string password = "master password";
        const string email = "user@example.test";
        var kdf = new KdfConfig.Pbkdf2(2);
        using MasterKey masterKey = MasterKey.Derive(password, email, kdf);
        using StretchedMasterKey stretchedMasterKey = masterKey.Stretch();
        byte[] expectedUserKey = [.. Enumerable.Range(128, 32).Select(static value => (byte)value)];
        EncString protectedValue = EncString.Encrypt(expectedUserKey, stretchedMasterKey.Span);

        byte[] actualUserKey = ProtectedUserKey.Create(protectedValue).Decrypt(password, email, kdf);

        try
        {
            Assert.Equal(expectedUserKey, actualUserKey);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(expectedUserKey);
            CryptographicOperations.ZeroMemory(actualUserKey);
        }
    }

    [Fact]
    public void Unlocked_user_key_imports_its_protected_private_key()
    {
        using RSA rsa = CreateRsa();
        byte[] privateKeyBytes = rsa.ExportPkcs8PrivateKey();
        using var userKey = new UnlockedUserKey(
            UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            CryptoTestFixtures.CombinedKey());
        try
        {
            EncString encryptedPrivateKey = EncString.Encrypt(privateKeyBytes, userKey.Key);
            using (RsaPrivateKey createdKey = userKey.CreatePrivateKey(ProtectedPrivateKey.Create(encryptedPrivateKey)))
            {
                byte[] plaintext = [9, 8, 7];
                AsymmetricEncString wrapped = Wrap(rsa, plaintext, EncryptionType.Rsa2048_OaepSha256_B64);
                Assert.Equal(plaintext, createdKey.Decrypt(wrapped));
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKeyBytes);
        }
    }

    [Fact]
    public void Organization_key_decrypts_its_rsa_wrapped_symmetric_key_and_preserves_its_id()
    {
        using RSA rsa = CreateRsa();
        byte[] privateKeyBytes = rsa.ExportPkcs8PrivateKey();
        byte[] symmetricKey = CryptoTestFixtures.CombinedKey();
        OrganizationId organizationId = OrganizationId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        try
        {
            using RsaPrivateKey privateKey = RsaPrivateKey.Import(privateKeyBytes);
            AsymmetricEncString encryptedOrganizationKey = Wrap(
                rsa,
                symmetricKey,
                EncryptionType.Rsa2048_OaepSha256_B64);
            using OrganizationKey organizationKey = OrganizationKey.Create(
                organizationId,
                encryptedOrganizationKey,
                privateKey);

            Assert.Equal(organizationId, organizationKey.OrganizationId);
            Assert.Equal(symmetricKey, organizationKey.Key.ToArray());
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKeyBytes);
            CryptographicOperations.ZeroMemory(symmetricKey);
        }
    }

    private static AsymmetricEncString Wrap(RSA rsa, byte[] plaintext, EncryptionType type)
    {
        RSAEncryptionPadding padding = type switch
        {
            EncryptionType.Rsa2048_OaepSha256_B64 => RSAEncryptionPadding.OaepSHA256,
            EncryptionType.Rsa2048_OaepSha1_B64 => RSAEncryptionPadding.OaepSHA1,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
        byte[] ciphertext = rsa.Encrypt(plaintext, padding);
        return AsymmetricEncString.FromBytes(CryptoTestFixtures.Pack(
            type,
            ReadOnlySpan<byte>.Empty,
            ciphertext,
            ReadOnlySpan<byte>.Empty));
    }

    private static void CreateCipherKey(EncString encryptedKey, SymmetricCryptoKey baseKey)
    {
        using CipherKey cipherKey = CipherKey.Create(encryptedKey, baseKey);
        _ = cipherKey.BorrowKeyMemory();
    }

    private static RSA CreateRsa()
    {
        RSA rsa = RSA.Create();
        rsa.KeySize = 2048;
        return rsa;
    }
}
