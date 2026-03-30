using BitwardenApi.Shared.Cryptography;
using FluentBitwarden.Modules.Security.Crypto.Kdf;
using System.Diagnostics;
using System.Security.Cryptography;

namespace FluentBitwarden.Modules.Session.Services;

internal static class SessionCrypto
{
    public static string HashMasterPassword(
        string email,
        string masterPassword,
        KdfConfig kdfConfig)
    {
        string normalizedEmail = NormalizeText(email);
        string salt = normalizedEmail;

        Span<byte> masterKey = stackalloc byte[32];

        switch (kdfConfig)
        {
            case KdfConfig.Pbkdf2 pbkdf2:
                Pbkdf2Kdf.Derive(masterPassword, salt, pbkdf2.Iterations, masterKey);
                break;
            case KdfConfig.Argon2Id argon2Id:
                Argon2IdKdf.Derive(masterPassword, salt, argon2Id.Iterations, argon2Id.MemoryMib, argon2Id.Parallelism, masterKey);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(kdfConfig));
        }

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

    public static string NormalizeText(ReadOnlySpan<char> email)
    {
        Span<char> span = stackalloc char[email.Length];
        int result = email.Trim().ToLowerInvariant(span);
        Debug.Assert(result >= 0);

        return span.ToString();
    }
}
