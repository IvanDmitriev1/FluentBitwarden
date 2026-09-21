using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;

namespace FluentBitwarden.AppHost.AppSession.Internal;

internal sealed class UnlockedSessionState(
    AccountProfile accountProfile,
    IUnlockedVault vault) : IDisposable
{
    private sealed class Lease(UnlockedSessionState instance) : IUnlockedSessionLease
    {
        private UnlockedSessionState? _state = instance;

        private UnlockedSessionState State =>
            Volatile.Read(ref _state)
            ?? throw new ObjectDisposedException(nameof(Lease));

        public AccountProfile Account => State.Account;
        public IUnlockedVault Vault => State.Vault;

        public void Dispose()
        {
            Interlocked.Exchange(ref _state, null)?.ReleaseLease();
        }
    }

    private readonly Lock _sync = new();
    private int _activeLeaseCount;
    private bool _disposeRequested;
    private bool _disposed;

    private bool ShouldDispose => _disposeRequested &&
                                  _activeLeaseCount == 0 &&
                                  !_disposed;

    public AccountProfile Account { get; } = accountProfile;
    public IUnlockedVault Vault { get; } = vault;

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
        ObjectDisposedException.ThrowIf(_disposeRequested, nameof(UnlockedSessionState));

        _activeLeaseCount++;

        return new Lease(this);
    }

    private void ReleaseLease()
    {
        using var _ = _sync.EnterScope();
        ObjectDisposedException.ThrowIf(_disposed, nameof(UnlockedSessionState));

        if (_activeLeaseCount <= 0)
            throw new InvalidOperationException("The session lease count is invalid.");

        _activeLeaseCount--;

        if (ShouldDispose)
            DisposeCore();
    }

    private void DisposeCore()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(UnlockedSessionState));

        Vault.Dispose();
        _disposed = true;
    }
}
