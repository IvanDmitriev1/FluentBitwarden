using FluentBitwarden.Data.Migrations;
using FluentBitwarden.Modules.Account.Abstractions;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models.Unlock;
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
    [ObservableProperty]
    public partial bool IsLoading { get; private set; }

    [ObservableProperty]
    public partial string StatusText { get; private set; } = "Loading...";

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

    public async Task OnLoadingAsync(UnlockResult.Success param, CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);

        navigationService.NavigateTo<ShellPage>();
    }

    public void OnUnloading() { }
}
