using BitwardenApi.Cryptography.Enc;
using BitwardenApi.Cryptography.Kdf;
using CommunityToolkit.HighPerformance.Buffers;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace BitwardenApi.Cryptography;

public static class CryptographyService
{
    private const int MaxStackPlaintextByteCount = 512;

    public static string HashMasterPassword(
        ReadOnlySpan<char> email,
        ReadOnlySpan<char> masterPassword,
        KdfConfig kdfConfig)
    {
        Span<char> normalizedEmailOwner = stackalloc char[email.Length];
        int normalizedEmailLength = email.Trim().ToLowerInvariant(normalizedEmailOwner);
        ReadOnlySpan<char> normalizedEmail = normalizedEmailOwner[..normalizedEmailLength];

        Span<byte> masterKey = stackalloc byte[32];
        DeriveMasterKey(masterPassword, normalizedEmail, kdfConfig, masterKey);

        try
        {
            Span<byte> authHash = stackalloc byte[32];
            Pbkdf2Kdf.Derive(masterKey, masterPassword, 1, authHash);
            return Convert.ToBase64String(authHash);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(masterKey);
        }
    }

    public static int UnwrapSymmetricKey(ReadOnlySpan<char> encKey, DecryptedUserKey userKey, Span<byte> destination)
    {
        Span<byte> keyBuffer = stackalloc byte[encKey.Length];
        var status = Ascii.FromUtf16(encKey, keyBuffer, out int bytesWritten);
        if (status != OperationStatus.Done)
        {
            throw new FormatException("EncString contains non-ASCII characters.");
        }

        var parts = EncString.Parse(keyBuffer[..bytesWritten]);
        return AesCbcHmac.DecryptTo(parts, userKey.Key, destination);
    }

    public static byte[] DecryptUserKey(EncryptedUserKey encryptedUserKey, ReadOnlySpan<char> masterPassword, ReadOnlySpan<char> salt, KdfConfig kdfConfig)
    {
        Span<byte> stretchedMasterKey = stackalloc byte[64];
        StretchMasterKey(masterPassword, salt, kdfConfig, stretchedMasterKey);

        ReadOnlySpan<char> encryptedValue = encryptedUserKey.Value;
        int length = encryptedValue.Length;
        bool useStackAlloc = length <= MaxStackPlaintextByteCount;

        using var bufferOwner = useStackAlloc
            ? SpanOwner<byte>.Empty
            : SpanOwner<byte>.Allocate(length);

        Span<byte> buffer = useStackAlloc
            ? stackalloc byte[length]
            : bufferOwner.Span;

        var status = Ascii.FromUtf16(encryptedValue, buffer, out int bytesWritten);
        if (status != OperationStatus.Done)
        {
            throw new FormatException("EncString contains non-ASCII characters.");
        }

        var parsed = EncString.Parse(buffer[..bytesWritten]);
        return AesCbcHmac.Decrypt(parsed, stretchedMasterKey);
    }

    public static void StretchMasterKey(ReadOnlySpan<char> masterPassword, ReadOnlySpan<char> salt, KdfConfig kdfConfig, Span<byte> stretchedMasterKey)
    {
        Span<byte> masterKey = stackalloc byte[32];
        DeriveMasterKey(masterPassword, salt, kdfConfig, masterKey);

        Hkdf.Expand(masterKey, "enc", stretchedMasterKey[..32]);
        Hkdf.Expand(masterKey, "mac", stretchedMasterKey.Slice(32, 32));
    }

    private static void DeriveMasterKey(ReadOnlySpan<char> masterPassword, ReadOnlySpan<char> salt, KdfConfig kdfConfig, Span<byte> output)
    {
        switch (kdfConfig)
        {
            case KdfConfig.Pbkdf2 pbkdf2:
                Pbkdf2Kdf.Derive(masterPassword, salt, pbkdf2.Iterations, output);
                break;
            case KdfConfig.Argon2Id argon2Id:
                Argon2IdKdf.Derive(masterPassword, salt, argon2Id.Iterations, argon2Id.MemoryMib, argon2Id.Parallelism, output);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(kdfConfig));
        }
    }

    public static string DecryptString(in EncStringParts encString, ReadOnlySpan<byte> key)
    {
        int maxPlaintextLength = encString.Data.Length;
        bool useStack = maxPlaintextLength <= MaxStackPlaintextByteCount;

        using var plaintextOwner = useStack
            ? SpanOwner<byte>.Empty
            : SpanOwner<byte>.Allocate(maxPlaintextLength);

        Span<byte> plaintext = useStack
            ? stackalloc byte[maxPlaintextLength]
            : plaintextOwner.Span;

        try
        {
            int bytesWritten = AesCbcHmac.DecryptTo(encString, key, plaintext);
            return Encoding.UTF8.GetString(plaintext[..bytesWritten]);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }
}
