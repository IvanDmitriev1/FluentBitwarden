using System.Security.Cryptography;
using BitwardenApi.Infrastructure.Cryptography.Kdf;
using CommunityToolkit.HighPerformance.Buffers;

namespace BitwardenApi.Infrastructure.Cryptography;

/// <summary>
/// The 256-bit master key derived from the user's master password via the account KDF
/// (PBKDF2 or Argon2id, email as salt). Never leaves the client; it is stretched into a
/// <see cref="StretchedMasterKey"/> to decrypt the protected user key, or used to produce
/// the authentication-only <see cref="MasterPasswordHash"/>.
/// Owns pooled key material; the creator must dispose it (zeroes the buffer).
/// </summary>
public readonly ref struct MasterKey
{
    private const int KeyLength = 32;

    private readonly SpanOwner<byte> _owner;

    private MasterKey(SpanOwner<byte> owner) => _owner = owner;

    public static MasterKey Derive(
        ReadOnlySpan<char> masterPassword,
        ReadOnlySpan<char> salt,
        KdfConfig kdfConfig)
    {
        var owner = SpanOwner<byte>.Allocate(KeyLength);
        try
        {
            switch (kdfConfig)
            {
                case KdfConfig.Pbkdf2 pbkdf2:
                    Pbkdf2Kdf.Derive(masterPassword, salt, pbkdf2.Iterations, owner.Span);
                    break;
                case KdfConfig.Argon2Id argon2Id:
                    Argon2IdKdf.Derive(masterPassword, salt, argon2Id.Iterations, argon2Id.MemoryMib, argon2Id.Parallelism, owner.Span);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kdfConfig));
            }

            return new MasterKey(owner);
        }
        catch
        {
            CryptographicOperations.ZeroMemory(owner.Span);
            owner.Dispose();
            throw;
        }
    }

    /// <summary>Expands the master key into a 512-bit <see cref="StretchedMasterKey"/> via HKDF.</summary>
    public StretchedMasterKey Stretch() => StretchedMasterKey.FromMasterKey(_owner.Span);

    /// <summary>
    /// Produces the authentication-only master password hash sent to the server.
    /// This value never decrypts vault data.
    /// </summary>
    public MasterPasswordHash HashPassword(ReadOnlySpan<char> masterPassword)
    {
        Span<byte> authHash = stackalloc byte[KeyLength];
        try
        {
            Pbkdf2Kdf.Derive(_owner.Span, masterPassword, 1, authHash);
            return new MasterPasswordHash(Convert.ToBase64String(authHash));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(authHash);
        }
    }

    public void Dispose()
    {
        CryptographicOperations.ZeroMemory(_owner.Span);
        _owner.Dispose();
    }
}
