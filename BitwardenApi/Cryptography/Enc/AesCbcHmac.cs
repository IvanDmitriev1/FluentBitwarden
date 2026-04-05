using System.Security.Cryptography;

namespace BitwardenApi.Cryptography.Enc;

internal static class AesCbcHmac
{
    private const int IvByteLength = 16;
    private const int MacByteLength = 32;
    private const int EncryptionKeyByteLength = 32;
    private const int CombinedKeyByteLength = 64;

    public static byte[] Decrypt(in EncStringParts parts, ReadOnlySpan<byte> key) =>
        parts.Type switch
        {
            EncStringType.AesCbc256_B64 => DecryptAesCbcOnly(parts, key),
            EncStringType.AesCbc256_HmacSha256_B64 => DecryptAesCbcWithHmac(parts, key),
            _ => throw new CryptographicException($"Unsupported symmetric EncString type: {parts.Type}.")
        };

    public static int DecryptTo(in EncStringParts parts, ReadOnlySpan<byte> key, Span<byte> destination) =>
        parts.Type switch
        {
            EncStringType.AesCbc256_B64 => DecryptToAesCbcOnly(parts, key, destination),
            EncStringType.AesCbc256_HmacSha256_B64 => DecryptToAesCbcWithHmac(parts, key, destination),
            _ => throw new CryptographicException($"Unsupported symmetric EncString type: {parts.Type}.")
        };

    private static byte[] DecryptAesCbcOnly(in EncStringParts parts, ReadOnlySpan<byte> key)
    {
        ValidateIv(parts);

        if (key.Length < EncryptionKeyByteLength)
            throw new CryptographicException("A 32-byte AES key is required.");

        using var aes = Aes.Create();
        aes.SetKey(key[..EncryptionKeyByteLength]);

        return aes.DecryptCbc(parts.Data, parts.Iv, PaddingMode.PKCS7);
    }

    private static int DecryptToAesCbcOnly(
        in EncStringParts parts,
        ReadOnlySpan<byte> key,
        Span<byte> destination)
    {
        ValidateIv(parts);

        if (key.Length < EncryptionKeyByteLength)
            throw new CryptographicException("A 32-byte AES key is required.");

        return DecryptAesCbcPkcs7(parts.Data, key[..EncryptionKeyByteLength], parts.Iv, destination);
    }

    private static byte[] DecryptAesCbcWithHmac(in EncStringParts parts, ReadOnlySpan<byte> key)
    {
        ValidateIvAndMac(parts);

        if (key.Length < CombinedKeyByteLength)
            throw new CryptographicException("A 64-byte key is required for HMAC-protected EncStrings.");

        VerifyMac(parts, key.Slice(EncryptionKeyByteLength, MacByteLength));

        using var aes = Aes.Create();
        aes.SetKey(key[..EncryptionKeyByteLength]);

        return aes.DecryptCbc(parts.Data, parts.Iv, PaddingMode.PKCS7);
    }

    private static int DecryptToAesCbcWithHmac(
        in EncStringParts parts,
        ReadOnlySpan<byte> key,
        Span<byte> destination)
    {
        ValidateIvAndMac(parts);

        if (key.Length < CombinedKeyByteLength)
            throw new CryptographicException("A 64-byte key is required for HMAC-protected EncStrings.");

        VerifyMac(parts, key.Slice(EncryptionKeyByteLength, MacByteLength));

        return DecryptAesCbcPkcs7(parts.Data, key[..EncryptionKeyByteLength], parts.Iv, destination);
    }

    private static void VerifyMac(in EncStringParts parts, ReadOnlySpan<byte> macKey)
    {
        Span<byte> expectedMac = stackalloc byte[MacByteLength];

        using var hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, macKey);
        hmac.AppendData(parts.Iv);
        hmac.AppendData(parts.Data);

        int written = hmac.GetHashAndReset(expectedMac);
        if (written != MacByteLength ||
            !CryptographicOperations.FixedTimeEquals(expectedMac, parts.Mac))
        {
            throw new CryptographicException("EncString MAC validation failed.");
        }
    }

    private static int DecryptAesCbcPkcs7(
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> iv,
        Span<byte> destination)
    {
        

        using var aes = Aes.Create();
        aes.SetKey(key);

        if (!aes.TryDecryptCbc(ciphertext, iv, destination, out int written, PaddingMode.PKCS7))
        {
            if (destination.Length < ciphertext.Length)
            {
                throw new ArgumentException(
                    "Destination span was too small for the decrypted plaintext.", nameof(destination));
            }

            throw new CryptographicException("EncString AES-CBC decryption failed.");
        }

        destination[written..].Clear();
        return written;
    }

    private static void ValidateIv(in EncStringParts parts)
    {
        if (parts.Iv.IsEmpty)
            throw new CryptographicException("EncString IV is required.");

        if (parts.Iv.Length != IvByteLength)
            throw new CryptographicException("EncString IV length was invalid.");
    }

    private static void ValidateIvAndMac(in EncStringParts parts)
    {
        ValidateIv(parts);

        if (parts.Mac.IsEmpty)
            throw new CryptographicException("EncString MAC is required.");

        if (parts.Mac.Length != MacByteLength)
            throw new CryptographicException("EncString MAC length was invalid.");
    }
}