using System.Diagnostics;
using BitwardenApi.Models;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Infrastructure.Services.Abstractions;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Session.Models;
using FluentBitwarden.Modules.Vault.Abstractions;
using FluentBitwarden.UI.Controls.Lifecycle;
using FluentBitwarden.Views.Offline;
using FluentBitwarden.Views.Offline.Models;
using FluentBitwarden.Views.Shell;
using FluentBitwarden.Views.Unlock.Models;
using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.Views.Unlock;

public sealed partial class UnlockPageViewModel(
    INavigationService navigationService,
    IVaultService vaultService,
    IConnectivityService connectivityService) : ObservableObject, IPageLifecycleAware<UnlockPageParameter>
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
            case AccountUnlockOutcome.Success success:
                OnSuccessUnlock(success.UserKey);
                return;

            case AccountUnlockOutcome.RequiresOnlineReauth:
                OnRequiresOnlineReauth();
                return;
        }
    }

    private void OnRequiresOnlineReauth()
    {
        if (connectivityService.HasInternetAccess)
        {
            throw new NotSupportedException();
            //navigationService.NavigateTo<SetupPage>();
            return;
        }

        navigationService.NavigateTo<OfflinePage>(
            PageNavigationParameter.From(new OfflinePageParameter(OfflinePageReason.ReauthRequiresInternet)));
    }

    private void OnSuccessUnlock(DecryptedUserKey decryptedUserKey)
    {
        vaultService.LoadLocalVault();
        navigationService.NavigateTo<ShellPage>();
    }
}
