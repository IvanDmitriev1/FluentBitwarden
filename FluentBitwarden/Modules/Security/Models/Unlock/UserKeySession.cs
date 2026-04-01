using System.Security.Cryptography;
using BitwardenApi.Modules.Identity.Models;

namespace FluentBitwarden.Modules.Security.Models.Unlock;

public sealed class UserKeySession(UserId userId, UnlockMethod unlockedVia, byte[] userKey) : IDisposable
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

    public UnlockMethod UnlockedVia
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return unlockedVia;
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