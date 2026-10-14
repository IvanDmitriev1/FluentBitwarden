namespace BitwardenApi.Infrastructure.Cryptography;

public static class MasterPassword
{
    public static MasterPasswordHash HashMasterPassword(
        ReadOnlySpan<char> email,
        ReadOnlySpan<char> masterPassword,
        KdfConfig kdfConfig)
    {
        Span<char> normalizedEmailOwner = stackalloc char[email.Length];
        int normalizedEmailLength = email.Trim().ToLowerInvariant(normalizedEmailOwner);
        ReadOnlySpan<char> normalizedEmail = normalizedEmailOwner[..normalizedEmailLength];

        using var masterKey = MasterKey.Derive(masterPassword, normalizedEmail, kdfConfig);
        return masterKey.HashPassword(masterPassword);
    }
}
