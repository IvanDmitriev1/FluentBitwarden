using CommunityToolkit.Mvvm.Input;
using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock;
using FluentBitwarden.Application;

namespace FluentBitwarden.ViewModels.Accounts.Unlock;

public sealed partial class UnlockPageViewModel(
    IAppCoordinator appCoordinator) : ObservableObject, IPageLifecycleAware<UnlockPageParameter>
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
        ArgumentNullException.ThrowIfNull(SelectedAccount);

        switch (result)
        {
            case AccountUnlockOutcome.Success:
                appCoordinator.RefreshSession();
                break;

            case AccountUnlockOutcome.RequiresOnlineReauth:
                appCoordinator.RequireSignIn(SelectedAccount);
                break;
        }
    }
}
