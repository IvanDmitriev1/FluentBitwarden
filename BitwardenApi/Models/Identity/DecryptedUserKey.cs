using System.Security.Cryptography;

namespace BitwardenApi.Models;

public sealed class DecryptedUserKey(UserId userId, byte[] userKey) : IDisposable
{
    private bool _disposed;

    public UserId UserId
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return userId;
        }
    }

    public ReadOnlySpan<byte> Key
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return userKey;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        CryptographicOperations.ZeroMemory(userKey);
    }
}