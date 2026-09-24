using FluentBitwarden.Application.Abstractions;
using FluentBitwarden.Application.Models;
using FluentBitwarden.Contracts.AppSession;
using AppSessionState = FluentBitwarden.Contracts.AppSession.State.AppSessionState;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.Application.Implementations;

internal sealed class AppSessionResolver(
    IAccountClient accountClient,
    IAppSessionClient appSessionClient) : IAppSessionResolver
{
    public async Task<AppSessionResolution> ResolveAsync()
    {
        var accounts = await accountClient.GetAccountsAsync(new());
        AppSessionState state = await appSessionClient.GetStateAsync(new());

        if (state is AppSessionState.Unlocked unlocked)
        {
            return new AppSessionResolution.UnlockedResolution(unlocked.Account);
        }

        if (state is AppSessionState.NotAuthenticated || accounts.Length == 0)
        {
            return new AppSessionResolution.LoggedOutResolution();
        }

        AccountProfile selectedAccount = state is AppSessionState.Locked locked
            ? locked.Account
            : accounts[0];

        return new AppSessionResolution.LockedResolution(accounts, selectedAccount);
    }
}
