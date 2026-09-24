using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.Contracts.AppSession.State;

namespace FluentBitwarden.AppHost.AppSession.Internal;

internal sealed class ActiveSessionManager
{
    private abstract record SessionState
    {
        public sealed record NotAuthenticated : SessionState;
        public sealed record Locked(AccountProfile Account) : SessionState;
        public sealed record Unlocked(AccountProfile Account, UnlockedVaultLifetime Vault) : SessionState;
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly SemaphoreSlim _transitionGate = new(1, 1);
    private readonly Lock _stateLock = new();

    private SessionState _state = new SessionState.NotAuthenticated();

    private TaskCompletionSource _unlockedSignalTcs = NewSignal();

    public AppSessionState State
    {
        get
        {
            lock (_stateLock)
            {
                return _state switch
                {
                    SessionState.NotAuthenticated => new AppSessionState.NotAuthenticated(),
                    SessionState.Locked locked => new AppSessionState.Locked(locked.Account),
                    SessionState.Unlocked unlocked => new AppSessionState.Unlocked(unlocked.Account),
                    _ => throw new InvalidOperationException("Unknown session state.")
                };
            }
        }
    }

    public bool TryGetUnlockedAccount([NotNullWhen(true)] out AccountProfile? accountProfile)
    {
        lock (_stateLock)
        {
            accountProfile = _state is SessionState.Unlocked unlocked ? unlocked.Account : null;
            return accountProfile is not null;
        }
    }

    public IUnlockedSessionLease? TryAcquireUnlockedSessionLease()
    {
        lock (_stateLock)
        {
            if (_state is SessionState.Unlocked unlocked)
            {
                return unlocked.Vault.CreateLease(unlocked.Account);
            }

            return null;
        }
    }

    public async ValueTask<IUnlockedSessionLease> WaitUntilUnlockedAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            Task pending;
            lock (_stateLock)
            {
                if (_state is SessionState.Unlocked unlocked)
                    return unlocked.Vault.CreateLease(unlocked.Account);

                pending = _unlockedSignalTcs.Task;
            }

            await pending.WaitAsync(cancellationToken);
        }
    }

    public Transition? TryEnterTransition() =>
        _transitionGate.Wait(0)
            ? new Transition(this)
            : null;

    public async Task<Transition> EnterTransition(CancellationToken cancellationToken)
    {
        await _transitionGate.WaitAsync(cancellationToken);
        return new Transition(this);
    }

    private void UpdateState(Func<SessionState, SessionState> update)
    {
        TaskCompletionSource signal;
        UnlockedVaultLifetime? stateToDispose = null;
        UnlockedVaultLifetime? uncommittedVault = null;

        try
        {
            lock (_stateLock)
            {
                SessionState previous = _state;
                SessionState next = update.Invoke(previous);
                UnlockedVaultLifetime? previousVault = GetVault(previous);
                UnlockedVaultLifetime? nextVault = GetVault(next);
                if (!ReferenceEquals(previousVault, nextVault))
                    uncommittedVault = nextVault;

                ValidateState(next);

                if (!ReferenceEquals(previousVault, nextVault))
                {
                    stateToDispose = previousVault;
                }

                _state = next;
                uncommittedVault = null;

                signal = _unlockedSignalTcs;
                _unlockedSignalTcs = NewSignal();
            }
        }
        catch
        {
            uncommittedVault?.Dispose();
            throw;
        }

        stateToDispose?.Dispose();
        signal.TrySetResult();
    }

    private static UnlockedVaultLifetime? GetVault(SessionState state) =>
        state is SessionState.Unlocked unlocked ? unlocked.Vault : null;

    private static void ValidateState(SessionState state)
    {
        if (state is not SessionState.Unlocked unlocked)
            return;

        if (unlocked.Account is not { } account)
        {
            throw new InvalidOperationException("An unlocked vault must have an active account.");
        }

        if (unlocked.Vault.UserId != account.UserId)
        {
            throw new InvalidOperationException("The active account and unlocked vault must have the same user ID.");
        }
    }

    public sealed class Transition(ActiveSessionManager owner) : IDisposable
    {
        private ActiveSessionManager? _owner = owner;

        private ActiveSessionManager Owner =>
            Volatile.Read(ref _owner)
            ?? throw new ObjectDisposedException(nameof(Transition));

        public void Dispose()
        {
            Interlocked.Exchange(ref _owner, null)?._transitionGate.Release();
        }

        public void Unlock(AccountProfile account, UnlockedVaultLifetime unlockedVault) =>
            Owner.UpdateState(_ => new SessionState.Unlocked(account, unlockedVault));

        public void Lock() =>
            Owner.UpdateState(static state => state switch
            {
                SessionState.Unlocked unlocked => new SessionState.Locked(unlocked.Account),
                _ => state
            });

        public void SignOut() =>
            Owner.UpdateState(static _ => new SessionState.NotAuthenticated());
    }
}
