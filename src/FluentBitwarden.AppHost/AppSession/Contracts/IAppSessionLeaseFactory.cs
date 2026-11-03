using FluentBitwarden.Contracts.AppSession.State;

namespace FluentBitwarden.AppHost.AppSession.Contracts;

public interface IAppSessionLeaseFactory
{
    AppSessionStateSnapshot State { get; }

    IAppSessionLease CreateLease();

    ValueTask<IAppSessionLease> WaitUntilUnlockedAsync(CancellationToken cancellationToken);
}
