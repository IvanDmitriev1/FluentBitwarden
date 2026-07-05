using System.Security.Cryptography;
using CommunityToolkit.HighPerformance.Buffers;

namespace BitwardenApi.Vault.Cryptography;

/// <summary>
/// Decrypted per-attachment key from VaultCipherAttachmentDownloadResponse.ProtectedAttachmentKey.
/// Heap-allocated because attachment download/decryption is asynchronous; zeroed on Dispose.
/// </summary>
public sealed class AttachmentKey : IDisposable
{
    private const int MacByteLength = 32;
    private const int EncryptionKeyByteLength = 32;
    private const int KeyByteLength = 64;

    private readonly MemoryOwner<byte> _owner;
    private bool _disposed;

    private AttachmentKey(MemoryOwner<byte> owner) => _owner = owner;

    internal ReadOnlySpan<byte> Key
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _owner.Span;
        }
    }

    /// <summary>The 32-byte AES key half. Heap-backed, so it is safe to use across await points.</summary>
    internal ReadOnlySpan<byte> AesKey
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _owner.Span[..EncryptionKeyByteLength];
        }
    }

    /// <summary>The 32-byte HMAC key half. Heap-backed, so it is safe to use across await points.</summary>
    internal ReadOnlySpan<byte> MacKey
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _owner.Span.Slice(EncryptionKeyByteLength, MacByteLength);
        }
    }


    public static AttachmentKey Create(
        in EncString encryptedKey,
        CipherKey cipherKey)
    {
        var owner = MemoryOwner<byte>.Allocate(KeyByteLength);
        try
        {
            encryptedKey.DecodeTo(cipherKey.Key, owner.Span);
            return new AttachmentKey(owner);
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
        if (_disposed)
            return;

        _disposed = true;
        CryptographicOperations.ZeroMemory(_owner.Span);
        _owner.Dispose();
    }
}
