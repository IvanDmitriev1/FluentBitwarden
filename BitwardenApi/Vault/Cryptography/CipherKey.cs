using System.Security.Cryptography;
using BitwardenApi.Vault.Cryptography;
using CommunityToolkit.HighPerformance.Buffers;

namespace BitwardenApi.Vault.Cryptography;

/// <summary>
/// The effective decryption key for a single vault cipher: the base vault key when the cipher
/// has no individual key, otherwise the decrypted VaultCipherDto.ProtectedCipherKey.
/// Owns pooled key material; the creator must dispose it (zeroes the buffer).
/// </summary>
public readonly ref struct CipherKey
{
    private readonly SpanOwner<byte> _owner;
    private readonly int _length;

    private CipherKey(SpanOwner<byte> owner, int length)
    {
        _owner = owner;
        _length = length;
    }

    internal ReadOnlySpan<byte> Key => _owner.Span[.._length];

    public static CipherKey Create(in EncString encryptedKey, SymmetricCryptoKey baseKey)
    {
        if (encryptedKey.IsEmpty)
        {
            var baseKeyBytes = baseKey.Key;
            var baseOwner = SpanOwner<byte>.Allocate(baseKeyBytes.Length);
            baseKeyBytes.CopyTo(baseOwner.Span);
            return new CipherKey(baseOwner, baseKeyBytes.Length);
        }

        var owner = SpanOwner<byte>.Allocate(encryptedKey.MaxPlaintextByteCount);
        try
        {
            int bytesWritten = encryptedKey.DecodeTo(baseKey.Key, owner.Span);
            return new CipherKey(owner, bytesWritten);
        }
        catch
        {
            CryptographicOperations.ZeroMemory(owner.Span);
            owner.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        CryptographicOperations.ZeroMemory(_owner.Span);
        _owner.Dispose();
    }
}

public static class CipherKeyExtensions
{
    extension(in EncString value)
    {
        public string Decode(CipherKey key) => value.Decode(key.Key);
    }

    public static int DecodeEncStringInPlace(this Span<byte> encodedUtf8, CipherKey key)
        => encodedUtf8.DecodeEncStringInPlace(key.Key);
}
