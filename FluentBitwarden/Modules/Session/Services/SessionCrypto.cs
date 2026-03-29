using BitwardenApi.Shared.Cryptography;
using FluentBitwarden.Modules.Security.Crypto.Enc;
using FluentBitwarden.Modules.Security.Crypto.Kdf;
using System.Diagnostics;
using System.Security.Cryptography;

namespace FluentBitwarden.Modules.Session.Services;

internal static class SessionCrypto
{
    public static string HashMasterPassword(
        string email,
        string masterPassword,
        KdfConfig kdfConfig,
        string? salt = null)
    {
        string normalizedEmail = NormalizeText(email);
        string normalizedSalt = string.IsNullOrEmpty(salt) ? normalizedEmail : NormalizeText(salt);

        byte[] masterKey = kdfConfig switch
        {
            KdfConfig.Pbkdf2 pbkdf2 => Pbkdf2Kdf.Derive(masterPassword, normalizedSalt, pbkdf2.Iterations, 32),
            KdfConfig.Argon2Id argon2Id => Argon2IdKdf.Derive(
                masterPassword,
                normalizedSalt,
                argon2Id.Iterations,
                argon2Id.MemoryMib,
                argon2Id.Parallelism,
                32),
            _ => throw new ArgumentOutOfRangeException(nameof(kdfConfig))
        };

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


    public static byte[] DecryptUserKey(EncString encryptedUserKey, ReadOnlySpan<byte> stretchedMasterKey)
    {
        EncStringParts parsed = encryptedUserKey.Parse();
        return AesCbcHmac.Decrypt(parsed, stretchedMasterKey);
    }

    public static string NormalizeText(ReadOnlySpan<char> email)
    {
        Span<char> span = stackalloc char[email.Length];
        int result = email.Trim().ToLowerInvariant(span);
        Debug.Assert(result >= 0);

        return span.ToString();
    }
}
