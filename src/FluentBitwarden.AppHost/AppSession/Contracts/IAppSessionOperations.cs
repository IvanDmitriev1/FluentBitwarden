using FluentBitwarden.Contracts.AppSession.Unlock;

namespace FluentBitwarden.AppHost.AppSession.Contracts;

public interface IAppSessionOperations
{
    Task<SessionUnlockOutcome> UnlockAsync(SessionUnlockRequest request, CancellationToken cancellationToken);
    Task LockAsync(CancellationToken cancellationToken);
}
