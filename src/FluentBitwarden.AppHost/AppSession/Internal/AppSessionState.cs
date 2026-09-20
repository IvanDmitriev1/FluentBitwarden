using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.Contracts.AppSession.Status;

namespace FluentBitwarden.AppHost.AppSession.Internal;

internal sealed class AppSessionState
{
    private static TaskCompletionSource CreateSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly Lock _stateLock = new();
    private readonly SemaphoreSlim _transitionGate = new(1, 1);

    private TaskCompletionSource _unlockedSignal = CreateSignal();

    private AppSessionSnapshot _snapshot =
        new(AppSessionStatus.Locked, null, DateTimeOffset.UtcNow);

    private AppActiveSession? _activeSession;

    public AppSessionSnapshot Current
    {
        get
        {
            lock (_stateLock)
            {
                return _snapshot;
            }
        }
    }

    public async Task<UnlockedSessionView> WaitUntilUnlockedAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            Task unlockedTask;

            lock (_stateLock)
            {
                if (_activeSession is not null)
                    return _activeSession.ToUnlockedSessionView();

                unlockedTask = _unlockedSignal.Task;
            }

            await unlockedTask.WaitAsync(cancellationToken);
        }
    }



}
