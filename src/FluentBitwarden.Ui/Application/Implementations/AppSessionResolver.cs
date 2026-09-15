using FluentBitwarden.Application.Abstractions;
using FluentBitwarden.Application.Models;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Sessions;

namespace FluentBitwarden.Application.Implementations;

internal sealed class AppSessionResolver(
    IAccountsClient accountsClient,
    ISessionClient sessionClient) : IAppSessionResolver
{
    public async Task<AppSessionResolution> ResolveAsync()
    {
        var accounts = await accountsClient.GetAccountsAsync();
        var unlockedAccount = await sessionClient.GetUnlockedAccount();

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
