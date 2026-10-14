using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.AppSession.Internal;

internal sealed class ActiveSessionManager(IIpcEventPublisher eventPublisher)
{
    private abstract record SessionState
    {
        public sealed record NotAuthenticated : SessionState;
        public sealed record Locked(AccountProfile Account) : SessionState;
        public sealed record Unlocked(UnlockedVaultLifetime Vault) : SessionState;
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
                    SessionState.Unlocked unlocked => new AppSessionState.Unlocked(unlocked.Vault.Account),
                    _ => throw new InvalidOperationException("Unknown session state.")
                };
            }
        }
    }

    public IUnlockedSessionLease? TryAcquireUnlockedSessionLease()
    {
        lock (_stateLock)
        {
            if (_state is SessionState.Unlocked unlocked)
            {
                return unlocked.Vault.CreateLease();
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
                    return unlocked.Vault.CreateLease();

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

    private async Task UpdateStateAsync(Func<SessionState, SessionState> update)
    {
        TaskCompletionSource signal;
        UnlockedVaultLifetime? vaultToDispose;
        AppSessionState previousState;
        AppSessionState committedState;

        lock (_stateLock)
        {
            previousState = State;

            SessionState previous = _state;
            SessionState next = update(previous);

            UnlockedVaultLifetime? previousVault = GetVault(previous);
            vaultToDispose = ReferenceEquals(previousVault, GetVault(next))
                ? null
                : previousVault;

            _state = next;
            committedState = State;

            signal = _unlockedSignalTcs;
            _unlockedSignalTcs = NewSignal();
        }

        try
        {
            vaultToDispose?.Dispose();
        }
        finally
        {
            signal.TrySetResult();
        }

        if (previousState != committedState)
        {
            await eventPublisher.PublishAsync(new AppSessionStateChangedEvent(committedState));
        }
    }

    private static UnlockedVaultLifetime? GetVault(SessionState state) =>
        state is SessionState.Unlocked unlocked ? unlocked.Vault : null;

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

        public Task UnlockAsync(UnlockedVaultLifetime unlockedVault) =>
            Owner.UpdateStateAsync(_ => new SessionState.Unlocked(unlockedVault));

        public Task LockAsync() =>
            Owner.UpdateStateAsync(static state => state switch
            {
                SessionState.Unlocked unlocked =>
                    new SessionState.Locked(unlocked.Vault.Account),
                _ => state
            });

        public Task SignOutAsync() =>
            Owner.UpdateStateAsync(static _ => new SessionState.NotAuthenticated());
    }
}
