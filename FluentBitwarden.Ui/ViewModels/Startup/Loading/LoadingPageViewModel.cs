using Windows.Networking.Connectivity;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Views.Accounts.Login;
using FluentBitwarden.Views.Accounts.Unlock;
using FluentBitwarden.Views.Shell;
using FluentBitwarden.Services.Navigation;

namespace FluentBitwarden.ViewModels.Startup.Loading;

public partial class LoadingPageViewModel(
    INavigationService navigationService,
    IAccountsClient accountsClient,
    IUiHostedServiceStarter hostedServiceStarter)
    : ObservableObject, IPageLifecycleAware, IPageLifecycleAware<LoadingPageParameter>
{
    public Task OnLoadingAsync(CancellationToken cancellationToken) =>
        OnLoadingAsync(LoadingPageParameter.MainShell, cancellationToken);

    public async Task OnLoadingAsync(LoadingPageParameter parameter, CancellationToken cancellationToken)
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

        var unlockedAccount = await accountsClient.GetUnlockedAccount(cancellationToken);
        if (unlockedAccount is not null)
        {
            if (parameter.Target == StartupFlowTarget.RequestHost)
            {
                await hostedServiceStarter.EnsureStartedAsync();
                return;
            }

            navigationService.NavigateTo<ShellPage>();
            return;
        }

        navigationService.NavigateTo<UnlockPage>(
            PageNavigationParameter.From(new UnlockPageParameter(accounts, accounts[0], parameter.Target)));
    }

    public void OnUnloading() { }
}
