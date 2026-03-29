using FluentBitwarden.Data.Migrations;
using FluentBitwarden.Modules.Account.Abstractions;
using FluentBitwarden.Shared.Behaviors;
using FluentBitwarden.Shell.Navigation;
using FluentBitwarden.Views.Setup;

namespace FluentBitwarden.Views.Loading;

public partial class LoadingPageViewModel(
    INavigationService navigationService,
    IAccountRepository accountRepository,
    IDataInitializationService dataInitializationService) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty]
    public partial bool IsLoading { get; private set; }

    public async Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        await dataInitializationService.InitializeAsync(cancellationToken);

        var accounts = await accountRepository.GetAccountsAsync(cancellationToken);
        if (accounts.Count > 0)
        {
            return;
        }

        navigationService.NavigateTo<SetupPage>();
    }

    public Task OnUnloadingAsync()
    {
        return Task.CompletedTask;
    }
}
