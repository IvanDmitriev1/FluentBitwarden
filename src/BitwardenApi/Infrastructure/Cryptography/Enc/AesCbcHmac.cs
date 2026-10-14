using System.Security.Cryptography;

namespace BitwardenApi.Infrastructure.Cryptography.Enc;

internal static class AesCbcHmac
{
    private const int MaxStackByteCount = 512;
    private const int IvByteLength = 16;
    private const int MacByteLength = 32;
    private const int EncryptionKeyByteLength = 32;
    private const int CombinedKeyByteLength = 64;

    public static int EncryptTo(
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> iv,
        Span<byte> ciphertextDestination,
        Span<byte> macDestination)
    {
        ValidateIv(iv);

        if (key.Length < CombinedKeyByteLength)
            throw new CryptographicException("A 64-byte key is required for HMAC-protected EncStrings.");

        if (macDestination.Length < MacByteLength)
            throw new ArgumentException("MAC destination span was too small.", nameof(macDestination));

        int ciphertextLength = EncryptAesCbcPkcs7(
            plaintext,
            key[..EncryptionKeyByteLength],
            iv,
            ciphertextDestination);

        ComputeMac(
            key.Slice(EncryptionKeyByteLength, MacByteLength),
            iv,
            ciphertextDestination[..ciphertextLength],
            macDestination);

        return ciphertextLength;
    }

    private static int EncryptAesCbcPkcs7(
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> iv,
        Span<byte> destination)
    {
        using var aes = Aes.Create();
        aes.SetKey(key);

        if (!aes.TryEncryptCbc(plaintext, iv, destination, out int written, PaddingMode.PKCS7))
        {
            throw new ArgumentException(
                "Destination span was too small for the encrypted ciphertext.", nameof(destination));
        }

        return written;
    }

    private static void ComputeMac(
        ReadOnlySpan<byte> macKey,
        ReadOnlySpan<byte> iv,
        ReadOnlySpan<byte> ciphertext,
        Span<byte> destination)
    {
        using var hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, macKey);
        hmac.AppendData(iv);
        hmac.AppendData(ciphertext);
        hmac.GetHashAndReset(destination);
    }

    public static string Decrypt(in EncStringParts parts, ReadOnlySpan<byte> key)
    {
        int maxPlaintextLength = parts.Data.Length;
        bool useStack = maxPlaintextLength <= MaxStackByteCount;

        Span<byte> plaintext = useStack
            ? stackalloc byte[maxPlaintextLength]
            : new byte[maxPlaintextLength];

        try
        {
            int bytesWritten = DecryptTo(in parts, key, plaintext);
            return System.Text.Encoding.UTF8.GetString(plaintext[..bytesWritten]);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public static int DecryptTo(
        in EncStringParts parts,
        ReadOnlySpan<byte> key,
        Span<byte> destination) =>
        parts.Type switch
        {
            EncryptionType.AesCbc256_B64 => DecryptToAesCbcOnly(in parts, key, destination),
            EncryptionType.AesCbc256_HmacSha256_B64 => DecryptToAesCbcWithHmac(in parts, key, destination),
            _ => throw new CryptographicException($"Unsupported symmetric EncString type: {parts.Type}.")
        };

    public static byte[] DecryptToArray(
        in EncStringParts parts,
        ReadOnlySpan<byte> key) =>
        parts.Type switch
        {
            EncryptionType.AesCbc256_B64 => DecryptAesCbcOnlyToArray(in parts, key),
            EncryptionType.AesCbc256_HmacSha256_B64 => DecryptAesCbcWithHmacToArray(in parts, key),
            _ => throw new CryptographicException($"Unsupported symmetric EncString type: {parts.Type}.")
        };

    private static int DecryptToAesCbcOnly(
        in EncStringParts parts,
        ReadOnlySpan<byte> key,
        Span<byte> destination)
    {
        ValidateAesCbcOnly(in parts, key);
        return DecryptAesCbcPkcs7(parts.Data, key[..EncryptionKeyByteLength], parts.Iv, destination);
    }

    private static byte[] DecryptAesCbcOnlyToArray(
        in EncStringParts parts,
        ReadOnlySpan<byte> key)
    {
        ValidateAesCbcOnly(in parts, key);
        return DecryptAesCbcPkcs7ToArray(parts.Data, key[..EncryptionKeyByteLength], parts.Iv);
    }

    private static int DecryptToAesCbcWithHmac(
        in EncStringParts parts,
        ReadOnlySpan<byte> key,
        Span<byte> destination)
    {
        ValidateAesCbcWithHmac(in parts, key);
        return DecryptAesCbcPkcs7(parts.Data, key[..EncryptionKeyByteLength], parts.Iv, destination);
    }

    private static byte[] DecryptAesCbcWithHmacToArray(
        in EncStringParts parts,
        ReadOnlySpan<byte> key)
    {
        ValidateAesCbcWithHmac(in parts, key);
        return DecryptAesCbcPkcs7ToArray(parts.Data, key[..EncryptionKeyByteLength], parts.Iv);
    }

    private static void ValidateAesCbcOnly(
        in EncStringParts parts,
        ReadOnlySpan<byte> key)
    {
        ValidateIv(parts.Iv);

        if (key.Length < EncryptionKeyByteLength)
            throw new CryptographicException("A 32-byte AES key is required.");
    }

    private static void ValidateAesCbcWithHmac(
        in EncStringParts parts,
        ReadOnlySpan<byte> key)
    {
        ValidateIvAndMac(parts.Iv, parts.Mac);

        if (key.Length < CombinedKeyByteLength)
            throw new CryptographicException("A 64-byte key is required for HMAC-protected EncStrings.");

        VerifyMac(in parts, key.Slice(EncryptionKeyByteLength, MacByteLength));
    }

    private static void VerifyMac(
        in EncStringParts parts,
        ReadOnlySpan<byte> macKey)
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

    private static byte[] DecryptAesCbcPkcs7ToArray(
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> iv)
    {
        using var aes = Aes.Create();
        aes.SetKey(key);
        return aes.DecryptCbc(ciphertext, iv, PaddingMode.PKCS7);
    }

    private static void ValidateIv(ReadOnlySpan<byte> iv)
    {
        if (iv.IsEmpty)
            throw new CryptographicException("EncString IV is required.");

        if (iv.Length != IvByteLength)
            throw new CryptographicException("EncString IV length was invalid.");
    }

    private static void ValidateIvAndMac(ReadOnlySpan<byte> iv, ReadOnlySpan<byte> mac)
    {
        ValidateIv(iv);

        if (mac.IsEmpty)
            throw new CryptographicException("EncString MAC is required.");

        if (mac.Length != MacByteLength)
            throw new CryptographicException("EncString MAC length was invalid.");
    }
}