using FluentBitwarden.Infrastructure.Services.Abstractions;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.UI.Controls.Lifecycle;
using FluentBitwarden.Views.Offline;
using FluentBitwarden.Views.Offline.Models;
using FluentBitwarden.Views.Shell;
using FluentBitwarden.Views.LogIn;
using FluentBitwarden.Views.Unlock;
using FluentBitwarden.Views.Unlock.Models;
using FluentBitwarden.Infrastructure.Abstractions;

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

            navigationService.NavigateTo<LogInFlowPage>();
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
