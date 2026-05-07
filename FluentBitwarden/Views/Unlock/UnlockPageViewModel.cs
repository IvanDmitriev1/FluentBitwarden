using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Views.Offline;
using FluentBitwarden.Views.Offline.Models;
using FluentBitwarden.Views.Setup;
using FluentBitwarden.Views.Shell;
using FluentBitwarden.Views.Unlock.Models;
using System.Diagnostics.CodeAnalysis;
using BitwardenApi.Modules.Identity.Models;
using FluentBitwarden.Infrastructure.Services.Abstractions;
using FluentBitwarden.Modules.Vault.Abstractions;
using FluentBitwarden.Resources.Controls.Lifecycle;

namespace FluentBitwarden.Views.Unlock;

public sealed partial class UnlockPageViewModel(
    INavigationService navigationService,
    IVaultSyncService vaultSyncService,
    IConnectivityService connectivityService) : ObservableObject, IPageLifecycleAware<UnlockPageParameter>
{
    [ObservableProperty]
    public partial AccountProfile? SelectedAccount { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUnlockMethods))]
    public partial IReadOnlyList<UnlockOption> UnlockMethods { get; private set; } = [];

    public bool HasUnlockMethods => UnlockMethods.Count > 1;


    [MemberNotNull(nameof(SelectedAccount))]
    public Task OnLoadingAsync(UnlockPageParameter param, CancellationToken cancellationToken)
    {
        SelectedAccount = param.FavoriteAccountProfile;
        UnlockMethods = UnlockOption.CreateUnlockOptions(param.FavoriteAccountUnlockCapabilities);

        return Task.CompletedTask;
    }

    public void OnUnloading() { }

    [RelayCommand]
    private void VaultUnlockResult(UnlockResult result)
    {
        switch (result)
        {
            case UnlockResult.Success success:
                OnSuccessUnlock(success.UserKey);
                return;

            case UnlockResult.RequiresOnlineReauth:
                OnRequiresOnlineReauth();
                return;
        }
    }

    private void OnRequiresOnlineReauth()
    {
        if (connectivityService.HasInternetAccess)
        {
            navigationService.NavigateTo<SetupPage>();
            return;
        }

        navigationService.NavigateTo<OfflinePage>(
            PageNavigationParameter.From(new OfflinePageParameter(OfflinePageReason.ReauthRequiresInternet)));
    }

    private void OnSuccessUnlock(DecryptedUserKey decryptedUserKey)
    {
        vaultSyncService.LoadAllFromDb();
        navigationService.NavigateTo<ShellPage>();
    }
}
