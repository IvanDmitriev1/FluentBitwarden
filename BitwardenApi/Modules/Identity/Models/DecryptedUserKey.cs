using System.Security.Cryptography;

namespace BitwardenApi.Modules.Identity.Models;

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

    public DateTimeOffset UnlockedAt
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return field;
        }
    } = DateTimeOffset.UtcNow;

    public ReadOnlySpan<byte> Key => userKey;

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        CryptographicOperations.ZeroMemory(userKey);
    }
}