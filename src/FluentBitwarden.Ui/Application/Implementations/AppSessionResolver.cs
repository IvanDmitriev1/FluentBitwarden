using FluentBitwarden.Application.Abstractions;
using FluentBitwarden.Application.Models;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.Modules.Accounts;

namespace FluentBitwarden.Application.Implementations;

internal sealed class AppSessionResolver(
    IAccountsClient accountsClient,
    IAppSessionClient appSessionClient) : IAppSessionResolver
{
    public async Task<AppSessionResolution> ResolveAsync()
    {
        var accounts = await accountsClient.GetAccountsAsync();
        var unlockedAccount = await appSessionClient.GetUnlockedAccount();

        if (unlockedAccount is not null)
        {
            return new AppSessionResolution.UnlockedResolution(unlockedAccount);
        }

        if (accounts.Length == 0)
        {
            return new AppSessionResolution.LoggedOutResolution();
        }

        var selectedAccount =
            accounts.FirstOrDefault()
            ?? accounts[0];

        return new AppSessionResolution.LockedResolution(accounts, selectedAccount);
    }
}
