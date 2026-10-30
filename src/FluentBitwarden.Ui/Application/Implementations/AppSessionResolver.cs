using FluentBitwarden.Application.Abstractions;
using FluentBitwarden.Application.Models;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.AppSession.State;

namespace FluentBitwarden.Application.Implementations;

internal sealed class AppSessionResolver(
    IAppSessionClient appSessionClient) : IAppSessionResolver
{
    public async Task<AppSessionResolution> ResolveAsync()
    {
        var snapshot = await appSessionClient.GetStateAsync(new());

        return snapshot switch
        {
            AppSessionStateSnapshot.NotAuthenticated => new AppSessionResolution.LoggedOutResolution(),
            AppSessionStateSnapshot.Locked locked => new AppSessionResolution.LockedResolution(locked.Account),
            AppSessionStateSnapshot.Unlocked unlocked => new AppSessionResolution.UnlockedResolution(unlocked.Account),
            _ => throw new InvalidOperationException("Unexpected app session state.")
        };
    }
}
