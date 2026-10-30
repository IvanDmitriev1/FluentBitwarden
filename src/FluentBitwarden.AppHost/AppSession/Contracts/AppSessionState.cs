using BitwardenApi.Notifications.Abstractions;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.AppSession.State;

namespace FluentBitwarden.AppHost.AppSession.Contracts;

public abstract record AppSessionState : IAsyncDisposable
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

    public async ValueTask ReleaseAsync()
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
            // Only DisposeAsync() may retire that ownership.
            if (!_disposed && _references == 1)
            {
                throw new InvalidOperationException(
                    "Cannot release the store-owned reference.");
            }

            shouldDispose = --_references == 0;
        }

        if (!shouldDispose)
            return;

        try
        {
            await DisposeCoreAsync();
        }
        finally
        {
            _revoked.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            _disposed = true;
        }

        try
        {
            await _revoked.CancelAsync();
        }
        finally
        {
            await ReleaseAsync();
            GC.SuppressFinalize(this);
        }
    }

    protected virtual ValueTask DisposeCoreAsync() => ValueTask.CompletedTask;

    public sealed record NotAuthenticated : AppSessionState;

    public sealed record Locked(AccountProfile Account) : AppSessionState;

    public sealed record Unlocked : AppSessionState
    {
        public Unlocked(
            AccountProfile account,
            IUnlockedVault vault,
            IAccountKeySession accountKeySession,
            IServerNotificationsDispatcher serverNotificationsDispatcher)
        {
            if (account.UserId != accountKeySession.UserId)
                throw new InvalidOperationException("The unlocked vault and account key must match the selected account.");

            Account = account;
            Vault = vault;
            AccountKeySession = accountKeySession;
            ServerNotificationsDispatcher = serverNotificationsDispatcher;
        }

        public AccountProfile Account { get; }
        public IUnlockedVault Vault { get; }
        public IAccountKeySession AccountKeySession { get; }
        public IServerNotificationsDispatcher ServerNotificationsDispatcher { get; }

        public void Deconstruct(
            out AccountProfile account,
            out IUnlockedVault vault,
            out IAccountKeySession accountKeySession,
            out IServerNotificationsDispatcher serverNotificationsDispatcher) =>
            (account, vault, accountKeySession, serverNotificationsDispatcher) =
            (Account, Vault, AccountKeySession, ServerNotificationsDispatcher);

        protected override async ValueTask DisposeCoreAsync()
        {
            try
            {
                await ServerNotificationsDispatcher.DisposeAsync();
            }
            finally
            {
                try
                {
                    Vault.Dispose();
                }
                finally
                {
                    AccountKeySession.Dispose();
                }
            }
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
