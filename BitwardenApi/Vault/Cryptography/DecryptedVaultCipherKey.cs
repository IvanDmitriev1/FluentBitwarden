using System.Security.Cryptography;
using BitwardenApi.Vault.Cryptography;
using CommunityToolkit.HighPerformance.Buffers;

namespace BitwardenApi.Vault.Cryptography;

/// <summary>
/// The effective decryption key for a single vault cipher: the base vault key when the cipher
/// has no individual key, otherwise the decrypted VaultCipherDto.EncryptedKey.
/// Owns pooled key material; the creator must dispose it (zeroes the buffer).
/// </summary>
public readonly ref struct DecryptedVaultCipherKey
{
    private readonly SpanOwner<byte> _owner;
    private readonly int _length;

    private DecryptedVaultCipherKey(SpanOwner<byte> owner, int length)
    {
        _owner = owner;
        _length = length;
    }

    internal ReadOnlySpan<byte> Key => _owner.Span[.._length];

    public static DecryptedVaultCipherKey Create(in EncString encryptedKey, DecryptedVaultKey baseKey)
    {
        if (encryptedKey.IsEmpty)
        {
            var baseKeyBytes = baseKey.Key;
            var baseOwner = SpanOwner<byte>.Allocate(baseKeyBytes.Length);
            baseKeyBytes.CopyTo(baseOwner.Span);
            return new DecryptedVaultCipherKey(baseOwner, baseKeyBytes.Length);
        }

        var owner = SpanOwner<byte>.Allocate(encryptedKey.MaxPlaintextByteCount);
        try
        {
            int bytesWritten = encryptedKey.DecodeTo(baseKey.Key, owner.Span);
            return new DecryptedVaultCipherKey(owner, bytesWritten);
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

public static class DecryptedVaultCipherKeyExtensions
{
    extension(in EncString value)
    {
        public string Decode(DecryptedVaultCipherKey key) => value.Decode(key.Key);
    }

    public static int DecodeEncStringInPlace(this Span<byte> encodedUtf8, DecryptedVaultCipherKey key)
        => encodedUtf8.DecodeEncStringInPlace(key.Key);
}
