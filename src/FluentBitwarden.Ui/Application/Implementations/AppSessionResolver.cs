using FluentBitwarden.Application.Abstractions;
using FluentBitwarden.Application.Models;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.AppSession.Status;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.Application.Implementations;

internal sealed class AppSessionResolver(
    IAccountsClient accountsClient,
    IAppSessionClient appSessionClient) : IAppSessionResolver
{
    public async Task<AppSessionResolution> ResolveAsync()
    {
        var accounts = await accountsClient.GetAccountsAsync(new());
        AppSessionSnapshot snapshot = await appSessionClient.GetSnapshotAsync(new());

        if (snapshot.Status == AppSessionStatus.Unlocked && snapshot.CurrentAccount is { } unlockedAccount)
        {
            return new AppSessionResolution.UnlockedResolution(unlockedAccount);
        }

        if (snapshot.Status == AppSessionStatus.NotAuthenticated || accounts.Length == 0)
        {
            return new AppSessionResolution.LoggedOutResolution();
        }

        AccountProfile selectedAccount = snapshot.CurrentAccount
            ?? accounts[0];

        return new AppSessionResolution.LockedResolution(accounts, selectedAccount);
    }
}
