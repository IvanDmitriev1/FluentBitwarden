using System.Security.Cryptography;
using BitwaredApi.Models.Vault;
using BitwaredApi.Utils;
using CommunityToolkit.HighPerformance.Buffers;

namespace BitwaredApi.Crypto.Enc;

internal static class AesCbcHmac
{
    private const int IvByteLength = 16;
    private const int MacByteLength = 32;

    public static byte[] Decrypt(EncStringParts encString, ReadOnlySpan<byte> key)
    {
        int maxPlaintextLength = CryptoEncoding.GetBase64DecodedLength(encString.Data, "EncString ciphertext");
        byte[] plaintext = new byte[maxPlaintextLength];
        int bytesWritten = DecryptCore(encString, key, plaintext);

        if (bytesWritten == plaintext.Length)
        {
            return plaintext;
        }

        byte[] trimmed = new byte[bytesWritten];
        plaintext.AsSpan(0, bytesWritten).CopyTo(trimmed);
        CryptographicOperations.ZeroMemory(plaintext);
        return trimmed;
    }

    public static int DecryptTo(EncStringParts encString, ReadOnlySpan<byte> key, Span<byte> destination)
        => DecryptCore(encString, key, destination);

    private static int DecryptCore(EncStringParts encString, ReadOnlySpan<byte> key, Span<byte> destination)
    {
        int cipherByteLength = CryptoEncoding.GetBase64DecodedLength(encString.Data, "EncString ciphertext");
        using var cipherOwner = MemoryOwner<byte>.Allocate(cipherByteLength);
        Span<byte> cipherBytes = cipherOwner.Span[..cipherByteLength];

        _ = CryptoEncoding.DecodeBase64(encString.Data, cipherBytes, "EncString ciphertext");
        return encString.Type switch
        {
            EncStringType.AesCbc256_B64 => DecryptAesCbc(encString, cipherBytes, key[..32], destination),
            EncStringType.AesCbc256_HmacSha256_B64 => DecryptAesCbcHmac(encString, cipherBytes, key, destination),
            _ => throw new CryptographicException($"Unsupported symmetric EncString type: {encString.Type}."),
        };
    }

    private static int DecryptAesCbc(
        EncStringParts encString,
        ReadOnlySpan<byte> cipherBytes,
        ReadOnlySpan<byte> encryptionKey,
        Span<byte> destination)
    {
        if (encString.Iv.IsEmpty)
        {
            throw new CryptographicException("EncString IV is required.");
        }

        using var ivOwner = MemoryOwner<byte>.Allocate(IvByteLength);
        Span<byte> iv = ivOwner.Span[..IvByteLength];

        if (CryptoEncoding.GetBase64DecodedLength(encString.Iv, "EncString IV") != IvByteLength)
        {
            throw new CryptographicException("EncString IV length was invalid.");
        }

        _ = CryptoEncoding.DecodeBase64(encString.Iv, iv, "EncString IV");
        return DecryptAesCbcPkcs7(cipherBytes, encryptionKey, iv, destination);
    }

    private static int DecryptAesCbcHmac(
        EncStringParts encString,
        ReadOnlySpan<byte> cipherBytes,
        ReadOnlySpan<byte> key,
        Span<byte> destination)
    {
        if (key.Length < 64)
        {
            throw new CryptographicException("A 64-byte key is required for HMAC-protected EncStrings.");
        }

        if (encString.Iv.IsEmpty || encString.Mac.IsEmpty)
        {
            throw new CryptographicException("EncString IV and MAC are required.");
        }

        using var fixedBufferOwner = MemoryOwner<byte>.Allocate(IvByteLength + MacByteLength + MacByteLength);
        Span<byte> fixedBuffers = fixedBufferOwner.Span[..(IvByteLength + (2 * MacByteLength))];
        Span<byte> iv = fixedBuffers[..IvByteLength];
        Span<byte> mac = fixedBuffers.Slice(IvByteLength, MacByteLength);
        Span<byte> computedMac = fixedBuffers.Slice(IvByteLength + MacByteLength, MacByteLength);

        int macInputLength = IvByteLength + cipherBytes.Length;
        using var macInputOwner = MemoryOwner<byte>.Allocate(macInputLength);
        Span<byte> macInput = macInputOwner.Span[..macInputLength];

        if (CryptoEncoding.GetBase64DecodedLength(encString.Iv, "EncString IV") != IvByteLength)
        {
            throw new CryptographicException("EncString IV length was invalid.");
        }

        if (CryptoEncoding.GetBase64DecodedLength(encString.Mac, "EncString MAC") != MacByteLength)
        {
            throw new CryptographicException("EncString MAC length was invalid.");
        }

        _ = CryptoEncoding.DecodeBase64(encString.Iv, iv, "EncString IV");
        _ = CryptoEncoding.DecodeBase64(encString.Mac, mac, "EncString MAC");

        iv.CopyTo(macInput);
        cipherBytes.CopyTo(macInput[IvByteLength..]);
        HMACSHA256.HashData(key[32..64], macInput, computedMac);

        if (!CryptographicOperations.FixedTimeEquals(computedMac, mac))
        {
            throw new CryptographicException("EncString MAC validation failed.");
        }

        return DecryptAesCbcPkcs7(cipherBytes, key[..32], iv, destination);
    }

    private static int DecryptAesCbcPkcs7(
        ReadOnlySpan<byte> cipherBytes,
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> iv,
        Span<byte> destination)
    {
        if (destination.Length < cipherBytes.Length)
        {
            throw new ArgumentException("Destination span was too small for the decrypted plaintext.", nameof(destination));
        }

        byte[] keyBytes = new byte[key.Length];
        key.CopyTo(keyBytes);

        try
        {
            using var aes = Aes.Create();
            aes.Key = keyBytes;

            if (!aes.TryDecryptCbc(cipherBytes, iv, destination, out int outputLength, PaddingMode.PKCS7))
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
