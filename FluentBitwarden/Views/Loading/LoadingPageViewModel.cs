using Windows.Networking.Connectivity;
using FluentBitwarden.Contracts.Session.Abstractions;
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
    IAccountSessionManagerClient accountSessionManagerClient)
    : ObservableObject, IPageLifecycleAware
{
    public async Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        var response = await accountSessionManagerClient.GetAccounts();

        if (response.Accounts.Length <= 0)
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

        var favoriteAccount = response.Accounts[0];
        if (await accountSessionManagerClient.HasActiveSession())
        {
            navigationService.NavigateTo<ShellPage>();
            return;
        }

        navigationService.NavigateTo<UnlockPage>(
            PageNavigationParameter.From(new UnlockPageParameter(response.Accounts, favoriteAccount)));
    }

    public void OnUnloading() { }
}
