using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.AppSession.Unlock;

namespace FluentBitwarden.AppHost.AppSession.Contracts;

public interface IAppSessionService
{
    AppSessionState State { get; }

    IUnlockedSessionLease? TryAcquireUnlockedSessionLease();

    ValueTask<IUnlockedSessionLease> WaitUntilUnlockedAsync(CancellationToken cancellationToken);

    SessionUnlockOutcome Unlock(SessionUnlockRequest request);

    Task LockAsync(CancellationToken cancellationToken = default);
}
