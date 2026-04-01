using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Modules.Security.Models.Unlock;
using FluentBitwarden.Shared.Behaviors.Lifecycle;
using FluentBitwarden.Views.Setup;
using FluentBitwarden.Views.Shell;
using FluentBitwarden.Views.Shell.Navigation;
using FluentBitwarden.Views.Unlock;

namespace FluentBitwarden.Views.Loading;

public partial class LoadingPageViewModel(
    INavigationService navigationService,
    IUnitOfWorkFactory unitOfWorkFactory)
    : ObservableObject,
        IPageLifecycleAware,
        IPageLifecycleAware<UnlockResult.Success>
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

        navigationService.NavigateTo<UnlockPage>(PageNavigationParameter.From(accounts));
    }

    public Task OnLoadingAsync(UnlockResult.Success param, CancellationToken cancellationToken)
    {
        navigationService.NavigateTo<ShellPage>();
        return Task.CompletedTask;
    }

    public void OnUnloading() { }
}
