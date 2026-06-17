using System.Security.Cryptography;

using CommunityToolkit.HighPerformance.Buffers;

namespace BitwardenApi.Infrastructure.Cryptography.Enc;

internal static class AesCbcHmac
{
    private const int MaxStackByteCount = 512;
    private const int IvByteLength = 16;
    private const int MacByteLength = 32;
    private const int EncryptionKeyByteLength = 32;
    private const int CombinedKeyByteLength = 64;

    public static string Decrypt(in EncStringParts parts, ReadOnlySpan<byte> key)
    {
        int maxPlaintextLength = parts.Data.Length;
        bool useStack = maxPlaintextLength <= MaxStackByteCount;

        using var plaintextOwner = useStack
            ? SpanOwner<byte>.Empty
            : SpanOwner<byte>.Allocate(maxPlaintextLength);

        Span<byte> plaintext = useStack
            ? stackalloc byte[maxPlaintextLength]
            : plaintextOwner.Span;

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
            EncStringType.AesCbc256_B64 => DecryptToAesCbcOnly(in parts, key, destination),
            EncStringType.AesCbc256_HmacSha256_B64 => DecryptToAesCbcWithHmac(in parts, key, destination),
            _ => throw new CryptographicException($"Unsupported symmetric EncString type: {parts.Type}.")
        };

    private static int DecryptToAesCbcOnly(
        in EncStringParts parts,
        ReadOnlySpan<byte> key,
        Span<byte> destination)
    {
        ValidateIv(parts.Iv);

        if (key.Length < EncryptionKeyByteLength)
            throw new CryptographicException("A 32-byte AES key is required.");

        return DecryptAesCbcPkcs7(parts.Data, key[..EncryptionKeyByteLength], parts.Iv, destination);
    }

    private static int DecryptToAesCbcWithHmac(
        in EncStringParts parts,
        ReadOnlySpan<byte> key,
        Span<byte> destination)
    {
        ValidateIvAndMac(parts.Iv, parts.Mac);

        if (key.Length < CombinedKeyByteLength)
            throw new CryptographicException("A 64-byte key is required for HMAC-protected EncStrings.");

        VerifyMac(in parts, key.Slice(EncryptionKeyByteLength, MacByteLength));

        return DecryptAesCbcPkcs7(parts.Data, key[..EncryptionKeyByteLength], parts.Iv, destination);
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
