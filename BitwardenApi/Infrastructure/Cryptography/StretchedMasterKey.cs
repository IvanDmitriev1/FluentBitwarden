using System.Security.Cryptography;
using BitwardenApi.Infrastructure.Cryptography.Kdf;
using CommunityToolkit.HighPerformance.Buffers;

namespace BitwardenApi.Infrastructure.Cryptography;

/// <summary>
/// The 512-bit stretched master key: the <see cref="MasterKey"/> expanded via HKDF into a
/// 256-bit AES key (first half) and a 256-bit HMAC key (second half). Used to decrypt the
/// protected user key. Owns pooled key material; the creator must dispose it (zeroes the buffer).
/// </summary>
public readonly ref struct StretchedMasterKey
{
    private const int KeyLength = 64;

    private readonly SpanOwner<byte> _owner;

    private StretchedMasterKey(SpanOwner<byte> owner) => _owner = owner;

    internal static StretchedMasterKey FromMasterKey(ReadOnlySpan<byte> masterKey)
    {
        var owner = SpanOwner<byte>.Allocate(KeyLength);
        try
        {
            Hkdf.Expand(masterKey, "enc", owner.Span[..32]);
            Hkdf.Expand(masterKey, "mac", owner.Span.Slice(32, 32));
            return new StretchedMasterKey(owner);
        }
        catch
        {
            CryptographicOperations.ZeroMemory(owner.Span);
            owner.Dispose();
            throw;
        }
    }

    /// <summary>The full 512-bit key: first 256 bits AES key, last 256 bits HMAC key.</summary>
    public ReadOnlySpan<byte> Span => _owner.Span;

    public void Dispose()
    {
        CryptographicOperations.ZeroMemory(_owner.Span);
        _owner.Dispose();
    }
}
