using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;

namespace FluentBitwarden.AppHost.AppSession.Internal;

internal sealed class UnlockedVaultLifetime(IUnlockedVault vault, UnlockedUserKey accountKey) : IDisposable
{
    private sealed class Lease(UnlockedVaultLifetime owner, AccountProfile account) : IUnlockedSessionLease
    {
        private UnlockedVaultLifetime? _owner = owner;

        private UnlockedVaultLifetime Owner =>
            Volatile.Read(ref _owner)
            ?? throw new ObjectDisposedException(nameof(Lease));

        public AccountProfile Account { get; } = account;
        public IUnlockedVault Vault => Owner._vault;
        public UnlockedUserKey UnlockedUserKey => Owner._accountKey;

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

    private readonly IUnlockedVault _vault = vault;
    private readonly UnlockedUserKey _accountKey = accountKey;
    public UserId UserId => _vault.UserId;

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

    public IUnlockedSessionLease CreateLease(AccountProfile account)
    {
        using var _ = _sync.EnterScope();
        ObjectDisposedException.ThrowIf(_disposeRequested, nameof(UnlockedVaultLifetime));

        _activeLeaseCount++;

        return new Lease(this, account);
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
        _vault.Dispose();
    }
}
