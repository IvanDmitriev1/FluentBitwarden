using FluentBitwarden.Data.Migrations;
using FluentBitwarden.Modules.Account.Abstractions;
using FluentBitwarden.Modules.Security.Models.Unlock;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Shared.Behaviors.Lifecycle;
using FluentBitwarden.Shell;
using FluentBitwarden.Shell.Navigation;
using FluentBitwarden.Views.Setup;
using FluentBitwarden.Views.Unlock;

namespace FluentBitwarden.Views.Loading;

public partial class LoadingPageViewModel(
    INavigationService navigationService,
    IAccountRepository accountRepository,
    ISessionTokensStore sessionTokensStore,
    IDataInitializationService dataInitializationService)
    : ObservableObject,
        IPageLifecycleAware,
        IPageLifecycleAware<UnlockResult.Success>
{
    public async Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        await dataInitializationService.InitializeAsync(cancellationToken);

        var accounts = await accountRepository.GetAccountsAsync(cancellationToken);

        if (accounts.Count <= 0)
        {
            navigationService.NavigateTo<SetupPage>();
            return;
        }

        navigationService.NavigateTo<UnlockPage>(PageNavigationParameter.From(accounts));
    }

    public Task OnLoadingAsync(UnlockResult.Success param, CancellationToken cancellationToken)
    {
        navigationService.NavigateTo<ShellPage>();
        return Task.CompletedTask;
    }

    public void OnUnloading() { }
}
