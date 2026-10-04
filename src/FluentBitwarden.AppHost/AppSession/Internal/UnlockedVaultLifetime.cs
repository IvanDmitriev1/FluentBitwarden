using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;

namespace FluentBitwarden.AppHost.AppSession.Internal;

internal sealed class UnlockedVaultLifetime : IDisposable
{
    private sealed class Lease(UnlockedVaultLifetime owner) : IUnlockedSessionLease
    {
        private UnlockedVaultLifetime? _owner = owner;

        private UnlockedVaultLifetime Owner =>
            Volatile.Read(ref _owner)
            ?? throw new ObjectDisposedException(nameof(Lease));

        public AccountProfile Account => Owner.Account;
        public IUnlockedVault Vault => Owner._vault;
        public IAccountKeySession AccountKeySession => Owner._accountKey;

        public void Dispose()
        {
            Interlocked.Exchange(ref _owner, null)?.ReleaseLease();
        }
    }

    private readonly Lock _sync = new();
    private int _activeLeaseCount;
    private bool _disposeRequested;
    private bool _disposed;

    private bool ShouldDispose => _disposeRequested &&
                                  _activeLeaseCount == 0 &&
                                  !_disposed;

    private readonly IUnlockedVault _vault;
    private readonly IAccountKeySession _accountKey;

    public UnlockedVaultLifetime(
        AccountProfile account,
        IUnlockedVault vault,
        IAccountKeySession accountKeySession)
    {
        _vault = vault;
        _accountKey = accountKeySession;
        Account = account;

        if (Account.BitwardenAccountContext != _vault.AccountContext || Account.UserId != _accountKey.UserId)
        {
            throw new InvalidOperationException("The unlocked vault and account key must match the selected account.");
        }
    }

    public AccountProfile Account { get; }
    public UserId UserId => _vault.AccountContext.UserId;

    public void Dispose()
    {
        using var _ = _sync.EnterScope();

        if (_disposeRequested)
            return;

        _disposeRequested = true;
        if (ShouldDispose)
        {
            DisposeCore();
        }
    }

    public IUnlockedSessionLease CreateLease()
    {
        using var _ = _sync.EnterScope();
        ObjectDisposedException.ThrowIf(_disposeRequested, nameof(UnlockedVaultLifetime));

        _activeLeaseCount++;

        return new Lease(this);
    }

    private void ReleaseLease()
    {
        using var _ = _sync.EnterScope();
        ObjectDisposedException.ThrowIf(_disposed, nameof(UnlockedVaultLifetime));

        if (_activeLeaseCount <= 0)
            throw new InvalidOperationException("The vault lease count is invalid.");

        _activeLeaseCount--;

        if (ShouldDispose)
            DisposeCore();
    }

    private void DisposeCore()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(UnlockedVaultLifetime));

        _disposed = true;
        _accountKey.Dispose();
        _vault.Dispose();
    }
}
