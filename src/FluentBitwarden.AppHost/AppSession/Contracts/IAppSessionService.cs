using FluentBitwarden.Contracts.AppSession.Status;
using FluentBitwarden.Contracts.AppSession.Unlock;

namespace FluentBitwarden.AppHost.AppSession.Contracts;

public interface IAppSessionService
{
    AppSessionSnapshot Snapshot { get; }

    ValueTask<IUnlockedSessionLease> WaitUntilUnlockedAsync(CancellationToken cancellationToken = default);

    ValueTask<SessionUnlockOutcome> UnlockAsync(
        SessionUnlockRequest request,
        CancellationToken cancellationToken = default);

    ValueTask LockAsync(CancellationToken cancellationToken = default);
}
