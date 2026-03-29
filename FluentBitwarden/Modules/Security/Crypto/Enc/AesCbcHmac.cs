using CommunityToolkit.HighPerformance.Buffers;
using System.Security.Cryptography;

namespace FluentBitwarden.Modules.Security.Crypto.Enc;

internal static class AesCbcHmac
{
    private const int IvByteLength = 16;
    private const int MacByteLength = 32;

    public static byte[] Decrypt(EncStringParts parts, ReadOnlySpan<byte> key)
    {
        int maxPlaintextLength = Base64Decoder.GetDecodedByteCount(parts.Data, "EncString ciphertext");
        byte[] plaintext = new byte[maxPlaintextLength];
        int bytesWritten = DecryptCore(parts, key, plaintext);

        if (bytesWritten == plaintext.Length)
        {
            return plaintext;
        }

        byte[] trimmed = new byte[bytesWritten];
        plaintext.AsSpan(0, bytesWritten).CopyTo(trimmed);
        CryptographicOperations.ZeroMemory(plaintext);
        return trimmed;
    }

    public static int DecryptTo(EncStringParts parts, ReadOnlySpan<byte> key, Span<byte> destination)
        => DecryptCore(parts, key, destination);

    private static int DecryptCore(EncStringParts parts, ReadOnlySpan<byte> key, Span<byte> destination)
    {
        int ciphertextByteCount = Base64Decoder.GetDecodedByteCount(parts.Data, "EncString ciphertext");
        using var ciphertextOwner = MemoryOwner<byte>.Allocate(ciphertextByteCount);
        Span<byte> ciphertext = ciphertextOwner.Span[..ciphertextByteCount];

        _ = Base64Decoder.Decode(parts.Data, ciphertext, "EncString ciphertext");
        return parts.Type switch
        {
            EncStringType.AesCbc256_B64 => DecryptAesCbcOnly(parts, ciphertext, key[..32], destination),
            EncStringType.AesCbc256_HmacSha256_B64 => DecryptAesCbcWithHmac(parts, ciphertext, key, destination),
            _ => throw new CryptographicException($"Unsupported symmetric EncString type: {parts.Type}."),
        };
    }

    private static int DecryptAesCbcOnly(
        EncStringParts parts,
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> encryptionKey,
        Span<byte> destination)
    {
        if (parts.Iv.IsEmpty)
        {
            throw new CryptographicException("EncString IV is required.");
        }

        using var ivOwner = MemoryOwner<byte>.Allocate(IvByteLength);
        Span<byte> iv = ivOwner.Span[..IvByteLength];

        if (Base64Decoder.GetDecodedByteCount(parts.Iv, "EncString IV") != IvByteLength)
        {
            throw new CryptographicException("EncString IV length was invalid.");
        }

        _ = Base64Decoder.Decode(parts.Iv, iv, "EncString IV");
        return DecryptAesCbcPkcs7(ciphertext, encryptionKey, iv, destination);
    }

    private static int DecryptAesCbcWithHmac(
        EncStringParts parts,
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> key,
        Span<byte> destination)
    {
        if (key.Length < 64)
        {
            throw new CryptographicException("A 64-byte key is required for HMAC-protected EncStrings.");
        }

        if (parts.Iv.IsEmpty || parts.Mac.IsEmpty)
        {
            throw new CryptographicException("EncString IV and MAC are required.");
        }

        using var decodedMetadataOwner = MemoryOwner<byte>.Allocate(IvByteLength + MacByteLength + MacByteLength);
        Span<byte> decodedMetadata = decodedMetadataOwner.Span[..(IvByteLength + (2 * MacByteLength))];
        Span<byte> iv = decodedMetadata[..IvByteLength];
        Span<byte> providedMac = decodedMetadata.Slice(IvByteLength, MacByteLength);
        Span<byte> expectedMac = decodedMetadata.Slice(IvByteLength + MacByteLength, MacByteLength);

        int macPayloadLength = IvByteLength + ciphertext.Length;
        using var macPayloadOwner = MemoryOwner<byte>.Allocate(macPayloadLength);
        Span<byte> macPayload = macPayloadOwner.Span[..macPayloadLength];

        if (Base64Decoder.GetDecodedByteCount(parts.Iv, "EncString IV") != IvByteLength)
        {
            throw new CryptographicException("EncString IV length was invalid.");
        }

        if (Base64Decoder.GetDecodedByteCount(parts.Mac, "EncString MAC") != MacByteLength)
        {
            throw new CryptographicException("EncString MAC length was invalid.");
        }

        _ = Base64Decoder.Decode(parts.Iv, iv, "EncString IV");
        _ = Base64Decoder.Decode(parts.Mac, providedMac, "EncString MAC");

        iv.CopyTo(macPayload);
        ciphertext.CopyTo(macPayload[IvByteLength..]);
        HMACSHA256.HashData(key[32..64], macPayload, expectedMac);

        if (!CryptographicOperations.FixedTimeEquals(expectedMac, providedMac))
        {
            throw new CryptographicException("EncString MAC validation failed.");
        }

        return DecryptAesCbcPkcs7(ciphertext, key[..32], iv, destination);
    }

    private static int DecryptAesCbcPkcs7(
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> iv,
        Span<byte> destination)
    {
        if (destination.Length < ciphertext.Length)
        {
            throw new ArgumentException("Destination span was too small for the decrypted plaintext.", nameof(destination));
        }

        byte[] keyBytes = new byte[key.Length];
        key.CopyTo(keyBytes);

        try
        {
            using var aes = Aes.Create();
            aes.Key = keyBytes;

            if (!aes.TryDecryptCbc(ciphertext, iv, destination, out int outputLength, PaddingMode.PKCS7))
            {
                throw new CryptographicException("EncString AES-CBC decryption failed.");
            }

            destination[outputLength..].Clear();
            return outputLength;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyBytes);
        }
    }
}
