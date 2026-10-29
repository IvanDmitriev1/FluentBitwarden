using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.AppSession.State;

namespace FluentBitwarden.AppHost.AppSession.Contracts;

public abstract record AppSessionState : IDisposable
{
    private AppSessionState() {}

    private readonly Lock _lock = new();
    private bool _disposed;
    private int _references = 1;
    private readonly CancellationTokenSource _revoked = new();

    public CancellationToken Revoked => _revoked.Token;

    public void AddReference()
    {
        using var _ = _lock.EnterScope();

        ObjectDisposedException.ThrowIf(_disposed, this);
        _references = checked(_references + 1);
    }

    public void Release()
    {
        bool shouldDispose;

        lock (_lock)
        {
            if (_references == 0)
            {
                throw new InvalidOperationException(
                    "Reference count is already zero.");
            }

            // The initial reference belongs to the store.
            // Only Dispose() may retire that ownership.
            if (!_disposed && _references == 1)
            {
                throw new InvalidOperationException(
                    "Cannot release the store-owned reference.");
            }

            shouldDispose = --_references == 0;
        }

        if (!shouldDispose)
            return;

        Disposing(true);
        _revoked.Dispose();
    }

    public void Dispose()
    {
        using var _ = _lock.EnterScope();

        if (_disposed)
            return;

        _disposed = true;
        _revoked.Cancel();
        Release();

        GC.SuppressFinalize(this);
    }

    protected virtual void Disposing(bool disposing) { }

    public sealed record NotAuthenticated : AppSessionState;

    public sealed record Locked(AccountProfile Account) : AppSessionState;

    public sealed record Unlocked(
        AccountProfile Account,
        IUnlockedVault Vault,
        IAccountKeySession AccountKeySession) : AppSessionState
    {
        public IAccountKeySession AccountKeySession { get; } = Account.UserId == AccountKeySession.UserId
            ? AccountKeySession
            : throw new InvalidOperationException("The unlocked vault and account key must match the selected account.");

        protected override void Disposing(bool disposing)
        {
            if (!disposing)
                return;

            Vault.Dispose();
            AccountKeySession.Dispose();
        }
    }

    public AppSessionStateSnapshot ToSnapshot => this switch
    {
        NotAuthenticated => new AppSessionStateSnapshot.NotAuthenticated(),
        Locked locked => new AppSessionStateSnapshot.Locked(locked.Account),
        Unlocked unlocked => new AppSessionStateSnapshot.Unlocked(unlocked.Account),
        _ => throw new InvalidOperationException($"Unknown state: {GetType().Name}")
    };
}
