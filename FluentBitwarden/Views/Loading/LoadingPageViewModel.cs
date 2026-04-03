using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Modules.Security.Abstractions;
using FluentBitwarden.Shared.Behaviors.Lifecycle;
using FluentBitwarden.Views.Setup;
using FluentBitwarden.Views.Shell.Navigation;
using FluentBitwarden.Views.Unlock;
using FluentBitwarden.Views.Unlock.Models;

namespace FluentBitwarden.Views.Loading;

public partial class LoadingPageViewModel(
    INavigationService navigationService,
    IUnlockService unlockService,
    IUnitOfWorkFactory unitOfWorkFactory)
    : ObservableObject, IPageLifecycleAware
{
    public async Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        using var unitOfWork = unitOfWorkFactory.Create();
        var accounts = await Task.Run(unitOfWork.AccountRepository.GetAccounts, cancellationToken);

        if (accounts.Count <= 0)
        {
            navigationService.NavigateTo<SetupPage>();
            return;
        }

        var favoriteAccount = accounts[0];
        var favoriteAccountCapabilities = await unlockService.GetCapabilitiesAsync(favoriteAccount.UserId, cancellationToken);

        navigationService.NavigateTo<UnlockPage>(
            PageNavigationParameter.From(
                new UnlockPageParameter(accounts, favoriteAccount, favoriteAccountCapabilities)));
    }

    public void OnUnloading() { }
}
