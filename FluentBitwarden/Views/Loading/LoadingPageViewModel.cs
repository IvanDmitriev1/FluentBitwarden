using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Security.Abstractions;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Resources.Controls.Lifecycle;
using FluentBitwarden.Shared.Services.Abstractions;
using FluentBitwarden.Views.Offline;
using FluentBitwarden.Views.Offline.Models;
using FluentBitwarden.Views.Setup;
using FluentBitwarden.Views.Shell;
using FluentBitwarden.Views.Unlock;
using FluentBitwarden.Views.Unlock.Models;

namespace FluentBitwarden.Views.Loading;

public partial class LoadingPageViewModel(
    INavigationService navigationService,
    ICurrentSessionAccessor currentSessionAccessor,
    IUnlockService unlockService,
    IUnitOfWorkFactory unitOfWorkFactory,
    IConnectivityService connectivityService)
    : ObservableObject, IPageLifecycleAware
{
    public async Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<StoredAccount> accounts;
        using (var unitOfWork = unitOfWorkFactory.Create())
        {
            accounts = unitOfWork.AccountRepository.GetAccounts();
        }

        if (accounts.Count <= 0)
        {
            if (!connectivityService.HasInternetAccess)
            {
                navigationService.NavigateTo<OfflinePage>(
                    PageNavigationParameter.From(new OfflinePageParameter(OfflinePageReason.FirstSignInRequiresInternet)));
                return;
            }

            navigationService.NavigateTo<SetupPage>();
            return;
        }

        var favoriteAccount = accounts[0];
        if (currentSessionAccessor.IsAuthenticated)
        {
            navigationService.NavigateTo<ShellPage>();
            return;
        }

        var favoriteAccountCapabilities = await unlockService.GetCapabilitiesAsync(favoriteAccount.UserId, cancellationToken);
        navigationService.NavigateTo<UnlockPage>(
            PageNavigationParameter.From(
                new UnlockPageParameter(accounts, favoriteAccount, favoriteAccountCapabilities)));
    }

    public void OnUnloading() { }
}
