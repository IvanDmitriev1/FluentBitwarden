using Windows.Networking.Connectivity;
using FluentBitwarden.Contracts.Accounts;
using FluentBitwarden.UI.Controls.Lifecycle;
using FluentBitwarden.Views.Offline;
using FluentBitwarden.Views.Offline.Models;
using FluentBitwarden.Views.Shell;
using FluentBitwarden.Views.LogIn;
using FluentBitwarden.Views.Unlock;
using FluentBitwarden.Infrastructure.Abstractions;
using FluentBitwarden.Views.Passkey;

namespace FluentBitwarden.Views.Loading;

public partial class LoadingPageViewModel(
    INavigationService navigationService,
    IAccountsClient accountsClient)
    : ObservableObject, IPageLifecycleAware
{
    public async Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        var accounts = await accountsClient.GetAccountsAsync(cancellationToken);

        if (accounts.Length <= 0)
        {
            if (!NetworkInformation.HasInternetAccess)
            {
                navigationService.NavigateTo<OfflinePage>(
                    PageNavigationParameter.From(new OfflinePageParameter(OfflinePageReason.FirstSignInRequiresInternet)));
                return;
            }

            navigationService.NavigateTo<LogInFlowPage>();
            return;
        }

        var unlockedAccount = await accountsClient.GetUnlockedAccount();
        if (unlockedAccount is not null)
        {
            navigationService.NavigateTo<ShellPage>();
            return;
        }

        navigationService.NavigateTo<UnlockPage>(
            PageNavigationParameter.From(new UnlockPageParameter(accounts, accounts[0])));
    }

    public void OnUnloading() { }
}
