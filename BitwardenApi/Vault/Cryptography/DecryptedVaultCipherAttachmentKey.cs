using System.Security.Cryptography;
using CommunityToolkit.HighPerformance.Buffers;

namespace BitwardenApi.Vault.Cryptography;

/// <summary>
/// Decrypted per-attachment key from VaultCipherAttachmentDownloadResponse.EncryptedKey.
/// Heap-allocated because attachment download/decryption is asynchronous; zeroed on Dispose.
/// </summary>
public sealed class DecryptedVaultCipherAttachmentKey : IDisposable
{
    private readonly MemoryOwner<byte> _owner;
    private bool _disposed;

    private DecryptedVaultCipherAttachmentKey(MemoryOwner<byte> owner) => _owner = owner;

    internal ReadOnlySpan<byte> Key
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _owner.Span;
        }
    }

    public static DecryptedVaultCipherAttachmentKey Create(
        in EncString encryptedKey,
        DecryptedVaultCipherKey cipherKey)
    {
        var owner = MemoryOwner<byte>.Allocate(encryptedKey.MaxPlaintextByteCount);
        try
        {
            int bytesWritten = encryptedKey.DecodeTo(cipherKey.Key, owner.Span);
            return new DecryptedVaultCipherAttachmentKey(owner[..bytesWritten]);
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
