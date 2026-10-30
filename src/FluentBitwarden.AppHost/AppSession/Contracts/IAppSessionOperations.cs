using FluentBitwarden.Contracts.AppSession.Unlock;

namespace FluentBitwarden.AppHost.AppSession.Contracts;

public interface IAppSessionOperations
{
    Task AuthenticateAsync(AccountProfile account, CancellationToken cancellationToken);
    Task<SessionUnlockOutcome> UnlockAsync(SessionUnlockRequest request, CancellationToken cancellationToken);
    Task LockAsync(CancellationToken cancellationToken);
}
