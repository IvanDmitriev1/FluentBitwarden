using FluentBitwarden.Contracts.AppSession;

namespace FluentBitwarden.AppHost.AppSession.Contracts;

internal static class IAppSessionAccessorExtensions
{
    extension(IAppSessionLease lease)
    {
        public AppSessionState.Unlocked RequireUnlockedState() =>
            lease.AppSessionState as AppSessionState.Unlocked ?? throw new UnlockedSessionRequiredException();
    }
}
