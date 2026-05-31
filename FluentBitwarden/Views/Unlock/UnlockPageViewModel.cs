using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Infrastructure.Abstractions;
using FluentBitwarden.UI.Controls.Lifecycle;
using FluentBitwarden.Views.Offline;
using FluentBitwarden.Views.Offline.Models;
using FluentBitwarden.Views.Shell;
using System.Diagnostics.CodeAnalysis;
using Windows.Networking.Connectivity;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock.General;
using FluentBitwarden.Views.Passkey;

namespace FluentBitwarden.Views.Unlock;

public sealed partial class UnlockPageViewModel(
    INavigationService navigationService) : ObservableObject, IPageLifecycleAware<UnlockPageParameter>
{
    [ObservableProperty]
    public partial AccountProfile? SelectedAccount { get; private set; }

    [MemberNotNull(nameof(SelectedAccount))]
    public Task OnLoadingAsync(UnlockPageParameter param, CancellationToken cancellationToken)
    {
        SelectedAccount = param.FavoriteAccountProfile;

        return Task.CompletedTask;
    }

    public void OnUnloading() { }

    [RelayCommand]
    private void VaultUnlockResult(AccountUnlockOutcome result)
    {
        switch (result)
        {
            case AccountUnlockOutcome.Success:
                navigationService.NavigateTo<ShellPage>();
                return;

            case AccountUnlockOutcome.RequiresOnlineReauth:
                OnRequiresOnlineReauth();
                return;
        }
    }

    private void OnRequiresOnlineReauth()
    {
        if (NetworkInformation.HasInternetAccess)
        {
            throw new NotSupportedException();
            //navigationService.NavigateTo<SetupPage>();
            return;
        }

        navigationService.NavigateTo<OfflinePage>(
            PageNavigationParameter.From(new OfflinePageParameter(OfflinePageReason.ReauthRequiresInternet)));
    }
}
