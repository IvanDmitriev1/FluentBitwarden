using System.Security.Cryptography;

namespace BitwardenApi.Tests.Infrastructure;

internal sealed class AttachmentCryptoFixture : IDisposable
{
    private readonly byte[] _keyMaterial = [.. Enumerable.Range(64, 64).Select(static value => (byte)value)];

    public AttachmentCryptoFixture()
    {
        var userKey = new UnlockedUserKey(
            UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            [.. Enumerable.Range(0, 64).Select(static value => (byte)value)]);
        using (userKey)
        {
            CipherKey cipherKey = CipherKey.Create(EncString.Empty, userKey);
            try
            {
                byte[] wrappingIv = [.. Enumerable.Range(16, 16).Select(static value => (byte)value)];
                EncString protectedKey = EncryptEncString(_keyMaterial, cipherKey.BorrowKeyMemory().Span, wrappingIv);
                Key = AttachmentKey.Create(protectedKey, cipherKey);
            }
            finally
            {
                cipherKey.Dispose();
            }
        }
    }

    public AttachmentKey Key { get; }

    public byte[] EncryptAttachment(ReadOnlySpan<byte> plaintext)
    {
        byte[] iv = [.. Enumerable.Range(32, 16).Select(static value => (byte)value)];
        byte[] ciphertext = EncryptCbc(plaintext, _keyMaterial.AsSpan(0, 32), iv);
        byte[] mac = ComputeMac(_keyMaterial.AsSpan(32, 32), iv, ciphertext);
        byte[] file = new byte[1 + iv.Length + mac.Length + ciphertext.Length];
        file[0] = (byte)EncryptionType.AesCbc256_HmacSha256_B64;
        iv.CopyTo(file, 1);
        mac.CopyTo(file, 1 + iv.Length);
        ciphertext.CopyTo(file, 1 + iv.Length + mac.Length);
        return file;
    }

    public void Dispose()
    {
        Key.Dispose();
        CryptographicOperations.ZeroMemory(_keyMaterial);
    }

    private static EncString EncryptEncString(
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> iv)
    {
        byte[] ciphertext = EncryptCbc(plaintext, key[..32], iv);
        byte[] mac = ComputeMac(key[32..64], iv, ciphertext);
        return EncString.FromBytes(CryptoTestFixtures.Pack(
            EncryptionType.AesCbc256_HmacSha256_B64,
            iv,
            ciphertext,
            mac));
    }

    private static byte[] EncryptCbc(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv)
    {
        byte[] aesKey = key.ToArray();
        byte[] plaintextBytes = plaintext.ToArray();
        try
        {
            using Aes aes = Aes.Create();
            aes.Key = aesKey;
            aes.IV = iv.ToArray();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            using ICryptoTransform encryptor = aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(aesKey);
            CryptographicOperations.ZeroMemory(plaintextBytes);
        }
    }

    private static byte[] ComputeMac(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> ciphertext)
    {
        byte[] authenticatedData = new byte[iv.Length + ciphertext.Length];
        iv.CopyTo(authenticatedData);
        ciphertext.CopyTo(authenticatedData.AsSpan(iv.Length));
        try
        {
            return HMACSHA256.HashData(key, authenticatedData);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(authenticatedData);
        }
    }
}
