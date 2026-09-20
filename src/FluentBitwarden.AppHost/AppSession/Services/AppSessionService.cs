using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.AppSession.Internal;
using FluentBitwarden.Contracts.AppSession.Status;
using FluentBitwarden.Contracts.AppSession.Unlock;

namespace FluentBitwarden.AppHost.AppSession.Services;

internal sealed class AppSessionService(AppSessionState appSessionState) : IAppSessionService
{
    public AppSessionSnapshot Snapshot => appSessionState.Current;

    public Task<UnlockedSessionView> WaitUntilUnlockedAsync(CancellationToken cancellationToken = default) =>
        appSessionState.WaitUntilUnlockedAsync(cancellationToken);

    public ValueTask<SessionUnlockOutcome> UnlockAsync(SessionUnlockRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask LockAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
