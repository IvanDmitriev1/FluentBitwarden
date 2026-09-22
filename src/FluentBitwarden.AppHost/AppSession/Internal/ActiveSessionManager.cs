using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.Contracts.AppSession.Status;

namespace FluentBitwarden.AppHost.AppSession.Internal;

internal sealed class ActiveSessionManager
{
    private sealed record SessionState(
        AccountProfile? Account,
        UnlockedSessionState? UnlockedState,
        DateTimeOffset ChangedAt);

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly SemaphoreSlim _transitionGate = new(1, 1);
    private readonly Lock _stateLock = new();

    private SessionState _state = new(
        Account: null,
        UnlockedState: null,
        ChangedAt: DateTimeOffset.UtcNow);

    private TaskCompletionSource _unlockedSignalTcs = NewSignal();

    public AppSessionSnapshot Snapshot
    {
        get
        {
            lock (_stateLock)
            {
                var status = _state switch
                {
                    { UnlockedState: not null } => AppSessionStatus.Unlocked,
                    { Account: not null } => AppSessionStatus.Locked,
                    _ => AppSessionStatus.NotAuthenticated
                };

                return new AppSessionSnapshot(status, _state.Account, _state.ChangedAt);
            }
        }
    }


    public async ValueTask<IUnlockedSessionLease> WaitUntilUnlockedAsync(CancellationToken ct = default)
    {
        while (true)
        {
            Task pending;
            lock (_stateLock)
            {
                if (_state?.UnlockedState is not null)
                    return _state.UnlockedState.CreateLease();

                pending = _unlockedSignalTcs.Task;
            }

            await pending.WaitAsync(ct).ConfigureAwait(false);
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
        UnlockedSessionState? stateToDispose;

        lock (_stateLock)
        {
            var previous = _state;
            var next = update.Invoke(previous);

            stateToDispose = _state.UnlockedState;

            if (next.UnlockedState is not null && next.Account is null)
            {
                throw new InvalidOperationException(
                    "An unlocked session must have an account.");
            }

            _state = next with
            {
                ChangedAt = DateTimeOffset.UtcNow
            };

            signal = _unlockedSignalTcs;
            _unlockedSignalTcs = NewSignal();
        }

        stateToDispose?.Dispose();
        signal.TrySetResult();
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

        public void Unlock(UnlockedSessionState newState) =>
            Owner.UpdateState(state => state with
            {
                UnlockedState = newState,
                Account = newState.Account
            });

        public void Lock() =>
            Owner.UpdateState(static state => state with
            {
                UnlockedState = null
            });

        public void SignOut() =>
            Owner.UpdateState(static state => state with
            {
                Account = null,
                UnlockedState = null
            });
    }
}
