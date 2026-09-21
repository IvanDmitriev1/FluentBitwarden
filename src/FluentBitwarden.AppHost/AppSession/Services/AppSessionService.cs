using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.AppSession.Internal;
using FluentBitwarden.Contracts.AppSession.Status;
using FluentBitwarden.Contracts.AppSession.Unlock;

namespace FluentBitwarden.AppHost.AppSession.Services;

internal sealed class AppSessionService(ActiveSessionManager activeSessionManager) : IAppSessionService
{
    public AppSessionSnapshot Snapshot => activeSessionManager.Snapshot;

    public ValueTask<IUnlockedSessionLease> WaitUntilUnlockedAsync(CancellationToken cancellationToken = default)
        => activeSessionManager.WaitUntilUnlockedAsync(cancellationToken);

    public async ValueTask<SessionUnlockOutcome> UnlockAsync(SessionUnlockRequest request, CancellationToken cancellationToken = default)
    {
        using var transition = activeSessionManager.TryEnterTransition();
        if (transition is null)
            throw new OperationCanceledException("Concurrent session unlock attempt detected.");

        throw new NotImplementedException();
    }

    public async ValueTask LockAsync(CancellationToken cancellationToken = default)
    {
        using var transition = await activeSessionManager.EnterTransition(cancellationToken);
        transition.Lock();
    }
}
