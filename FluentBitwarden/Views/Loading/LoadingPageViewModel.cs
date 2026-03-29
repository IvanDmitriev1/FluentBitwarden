using CommunityToolkit.Mvvm.Messaging;
using FluentBitwarden.Data.Migrations;
using FluentBitwarden.Modules.Account.Abstractions;
using FluentBitwarden.Shared.Behaviors.PageLyfecycle;
using FluentBitwarden.Shell.Navigation;
using FluentBitwarden.Views.Setup;

namespace FluentBitwarden.Views.Loading;

public partial class LoadingPageViewModel(
    INavigationService navigationService,
    IAccountRepository accountRepository,
    IDataInitializationService dataInitializationService,
    IMessenger messenger)
    : ObservableRecipient(messenger), IPageLifecycleAware
{
    [ObservableProperty]
    public partial bool IsLoading { get; private set; }

    [ObservableProperty]
    public partial string StatusText { get; private set; } = "Loading...";

    public async Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        await dataInitializationService.InitializeAsync(cancellationToken);

        var accounts = await accountRepository.GetAccountsAsync(cancellationToken);
        var shouldNavigateToSetup = accounts.Count == 0;

        if (!shouldNavigateToSetup)
        {
            return;
        }

        navigationService.NavigateTo<SetupPage>();
    }

    public void OnUnloading() { }
}
