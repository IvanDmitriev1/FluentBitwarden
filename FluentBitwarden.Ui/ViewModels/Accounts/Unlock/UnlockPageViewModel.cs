using CommunityToolkit.Mvvm.Input;
using System.Diagnostics.CodeAnalysis;
using Windows.Networking.Connectivity;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock;
using FluentBitwarden.Views.Shell;
using FluentBitwarden.Views.Startup;

namespace FluentBitwarden.ViewModels.Accounts.Unlock;

public sealed partial class UnlockPageViewModel(
    INavigationService navigationService) : ObservableObject, IPageLifecycleAware<UnlockPageParameter>
{
    private StartupFlowTarget _startupTarget = StartupFlowTarget.MainShell;

    [ObservableProperty]
    public partial AccountProfile? SelectedAccount { get; private set; }

    [MemberNotNull(nameof(SelectedAccount))]
    public Task OnLoadingAsync(UnlockPageParameter param, CancellationToken cancellationToken)
    {
        SelectedAccount = param.FavoriteAccountProfile;
        _startupTarget = param.StartupTarget;

        return Task.CompletedTask;
    }

    public void OnUnloading() { }

    [RelayCommand]
    private void VaultUnlockResult(AccountUnlockOutcome result)
    {
        switch (result)
        {
            case AccountUnlockOutcome.Success:
                if (_startupTarget == StartupFlowTarget.RequestHost)
                {
                    navigationService.NavigateTo<LoadingPage>(
                        PageNavigationParameter.From(LoadingPageParameter.RequestHost));
                    break;
                }

                navigationService.NavigateTo<ShellPage>();
                break;

            case AccountUnlockOutcome.RequiresOnlineReauth:
                OnRequiresOnlineReauth();
                break;
        }
    }

    private void OnRequiresOnlineReauth()
    {
        if (NetworkInformation.HasInternetAccess)
        {
            throw new NotSupportedException();
            //navigationService.NavigateTo<SetupPage>();
        }

        navigationService.NavigateTo<OfflinePage>(
            PageNavigationParameter.From(new OfflinePageParameter(OfflinePageReason.ReauthRequiresInternet)));
    }
}
