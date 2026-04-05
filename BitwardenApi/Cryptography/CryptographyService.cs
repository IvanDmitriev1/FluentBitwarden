using BitwardenApi.Cryptography.Enc;
using BitwardenApi.Cryptography.Kdf;
using CommunityToolkit.HighPerformance.Buffers;
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

    public static byte[] DecryptUserKey(in EncryptedUserKey encryptedUserKey, ReadOnlySpan<char> masterPassword, ReadOnlySpan<char> salt, KdfConfig kdfConfig)
    {
        Span<byte> stretchedMasterKey = stackalloc byte[64];
        StretchMasterKey(masterPassword, salt, kdfConfig, stretchedMasterKey);

        using var encString = EncString.From(encryptedUserKey.Value);
        var parsed = encString.Parse();
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

    public static string DecryptString(in EncStringParts encString, DecryptedUserKey key)
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
            int bytesWritten = AesCbcHmac.DecryptTo(encString, key.Key, plaintext);
            return Encoding.UTF8.GetString(plaintext[..bytesWritten]);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }
}