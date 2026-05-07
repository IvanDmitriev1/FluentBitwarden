using FluentBitwarden.Infrastructure.Services.Abstractions;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Resources.Controls.Lifecycle;
using FluentBitwarden.Views.Offline;
using FluentBitwarden.Views.Offline.Models;
using FluentBitwarden.Views.Setup;
using FluentBitwarden.Views.Shell;
using FluentBitwarden.Views.Unlock;
using FluentBitwarden.Views.Unlock.Models;

namespace FluentBitwarden.Views.Loading;

public partial class LoadingPageViewModel(
    INavigationService navigationService,
    IAccountSessionManager accountSessionManager,
    IConnectivityService connectivityService)
    : ObservableObject, IPageLifecycleAware
{
    public Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        var accounts = accountSessionManager.GetAccounts();

        if (accounts.Count <= 0)
        {
            if (!connectivityService.HasInternetAccess)
            {
                navigationService.NavigateTo<OfflinePage>(
                    PageNavigationParameter.From(new OfflinePageParameter(OfflinePageReason.FirstSignInRequiresInternet)));
                return Task.CompletedTask;
            }

            navigationService.NavigateTo<SetupPage>();
            return Task.CompletedTask;
        }

        var favoriteAccount = accounts[0];
        if (accountSessionManager.ActiveSession is not null)
        {
            navigationService.NavigateTo<ShellPage>();
            return Task.CompletedTask;
        }

        navigationService.NavigateTo<UnlockPage>(
            PageNavigationParameter.From(new UnlockPageParameter(accounts, favoriteAccount)));

        return Task.CompletedTask;
    }

    public void OnUnloading() { }
}
