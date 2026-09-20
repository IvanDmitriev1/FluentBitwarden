using FluentBitwarden.Contracts.AppSession.Status;
using FluentBitwarden.Contracts.AppSession.Unlock;

namespace FluentBitwarden.AppHost.AppSession.Contracts;

public interface IAppSessionService
{
    AppSessionSnapshot Snapshot { get; }

    Task<UnlockedSessionView> WaitUntilUnlockedAsync(CancellationToken cancellationToken = default);

    ValueTask<SessionUnlockOutcome> UnlockAsync(
        SessionUnlockRequest request,
        CancellationToken cancellationToken = default);

    ValueTask LockAsync(CancellationToken cancellationToken = default);
}
