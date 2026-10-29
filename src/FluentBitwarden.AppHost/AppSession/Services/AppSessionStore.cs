using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.AppSession.Services;

internal sealed class AppSessionStore(IIpcEventPublisher eventPublisher) : IAppSessionLeaseProvider
{
    private readonly Lock _lock = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private static TaskCompletionSource<bool> NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private AppSessionState _state = new AppSessionState.NotAuthenticated();
    private TaskCompletionSource<bool> _changed = NewSignal();

    public AppSessionStateSnapshot State
    {
        get
        {
            lock (_lock)
            {
                return _state.ToSnapshot;
            }
        }
    }

    public IAppSessionAccessor CreateLease()
    {
        lock (_lock)
        {
            var state = _state;

            state.AddReference();

            return new AppSessionAccessor(state);
        }
    }

    public async ValueTask<IAppSessionAccessor> WaitUntilUnlockedAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            Task changed;

            lock (_lock)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_state is AppSessionState.Unlocked)
                {
                    return CreateLease();
                }

                changed = _changed.Task;
            }

            await changed.WaitAsync(cancellationToken);
        }
    }

    public async Task AuthenticateAsync(AccountProfile account, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);

        try
        {
            lock (_lock)
            {
                if (_state is not AppSessionState.NotAuthenticated)
                {
                    throw new InvalidOperationException(
                        "Session is already authenticated.");
                }
            }

            if (ct.IsCancellationRequested)
                return;

            await ChangeAsync(new AppSessionState.Locked(account));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UnlockAsync(AppSessionState.Unlocked nextState)
    {
        await _gate.WaitAsync();

        try
        {
            lock (_lock)
            {
                if (_state is not AppSessionState.Locked)
                    throw new InvalidOperationException("Session must be locked.");
            }

            await ChangeAsync(nextState);
        }
        catch
        {
            bool published;
            lock (_lock)
                published = ReferenceEquals(_state, nextState);

            if (!published)
                nextState.Dispose();

            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task LockAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);

        try
        {
            AppSessionState.Unlocked unlocked;

            lock (_lock)
            {
                if (_state is not AppSessionState.Unlocked current)
                    return;

                unlocked = current;
            }

            if (ct.IsCancellationRequested)
                return;

            await ChangeAsync(new AppSessionState.Locked(unlocked.Account));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SignOutAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);

        try
        {
            lock (_lock)
            {
                if (_state is AppSessionState.NotAuthenticated)
                    return;
            }

            if (ct.IsCancellationRequested)
                return;

            await ChangeAsync(new AppSessionState.NotAuthenticated());
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task ChangeAsync(AppSessionState next)
    {
        if (ReferenceEquals(_state, next))
            return;

        AppSessionState previous;

        lock (_lock)
        {
            previous = _state;
            _state = next;

            var signal = _changed;
            _changed = NewSignal();

            signal.TrySetResult(true);
        }

        try
        {
            await eventPublisher.PublishAsync(new AppSessionStateChangedEvent(next.ToSnapshot));
        }
        finally
        {
            previous.Dispose();
        }
    }
}
