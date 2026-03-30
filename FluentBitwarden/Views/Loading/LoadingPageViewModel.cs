using CommunityToolkit.Mvvm.Messaging;
using FluentBitwarden.Data.Migrations;
using FluentBitwarden.Modules.Account.Abstractions;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Shared.Behaviors.Lifecycle;
using FluentBitwarden.Shell.Navigation;
using FluentBitwarden.Views.Setup;
using FluentBitwarden.Views.Unlock;

namespace FluentBitwarden.Views.Loading;

public partial class LoadingPageViewModel(
    INavigationService navigationService,
    IAccountRepository accountRepository,
    ISessionTokensStore sessionTokensStore,
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

        if (accounts.Count <= 0)
        {
            navigationService.NavigateTo<SetupPage>();
            return;
        }

        navigationService.NavigateTo<UnlockPage>(accounts);
    }

    public void OnUnloading() { }
}
