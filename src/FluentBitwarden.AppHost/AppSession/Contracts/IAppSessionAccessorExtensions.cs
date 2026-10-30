using FluentBitwarden.Contracts.AppSession;

namespace FluentBitwarden.AppHost.AppSession.Contracts;

internal static class IAppSessionAccessorExtensions
{
    extension(IAppSessionAccessor accessor)
    {
        public AppSessionState.Unlocked RequireUnlockedState() =>
            accessor.AppSessionState as AppSessionState.Unlocked ?? throw new UnlockedSessionRequiredException();
    }
}
