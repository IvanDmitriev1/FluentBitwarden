using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Connectivity.Abstractions;
using FluentBitwarden.Modules.Security.Abstractions;
using FluentBitwarden.Modules.Security.Models.Unlock;
using FluentBitwarden.Modules.Security.Services.Unlock;
using FluentBitwarden.Resources.Controls;
using FluentBitwarden.Shared.Behaviors.Lifecycle;
using FluentBitwarden.Views.Offline;
using FluentBitwarden.Views.Offline.Models;
using FluentBitwarden.Views.Setup;
using FluentBitwarden.Views.Shell;
using FluentBitwarden.Views.Shell.Navigation;
using FluentBitwarden.Views.Unlock.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Modules.Vault.Abstractions;

namespace FluentBitwarden.Views.Unlock;

public sealed partial class UnlockPageViewModel(
    IUnlockService unlockService,
    INavigationService navigationService,
    IVaultSyncService vaultSyncService,
    IConnectivityService connectivityService) : ObservableValidator, IPageLifecycleAware<UnlockPageParameter>
{
    [ObservableProperty]
    public partial StoredAccount? SelectedAccount { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUnlockMethods))]
    public partial IReadOnlyList<UnlockOption> UnlockMethods { get; private set; } = [];

    public bool HasUnlockMethods => UnlockMethods.Count > 1;



    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Enter your master password.")]
    [CustomValidation(typeof(UnlockPageViewModel), nameof(ValidateMasterPassword))]
    public partial string Password { get; set; } = string.Empty;

    [field:AllowNull]
    public ValidatableProperty PasswordValidation
        => field ??= ValidatableProperty.Create(this, static state => state.Password);


    private UnlockResult.Failure? _invalidCredentials;


    [MemberNotNull(nameof(SelectedAccount))]
    public Task OnLoadingAsync(UnlockPageParameter param, CancellationToken cancellationToken)
    {
        SelectedAccount = param.FavoriteAccount;
        UnlockMethods = UnlockOption.CreateUnlockOptions(param.FavoriteAccountUnlockCapabilities);

        return Task.CompletedTask;
    }

    public void OnUnloading() { }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task UnlockMasterPassword()
    {
        var result = await unlockService.UnlockAsync(SelectedAccount!.UserId, new MasterPasswordUnlockRequest(Password));
        switch (result)
        {
            case UnlockResult.Success:
                _ = await vaultSyncService.SyncVaultAsync();
                navigationService.NavigateTo<ShellPage>();
                return;

            case UnlockResult.RequiresOnlineReauth:
                if (connectivityService.HasInternetAccess)
                {
                    navigationService.NavigateTo<SetupPage>();
                    return;
                }

                navigationService.NavigateTo<OfflinePage>(
                    PageNavigationParameter.From(new OfflinePageParameter(OfflinePageReason.ReauthRequiresInternet)));
                return;

            case UnlockResult.Failure failure:
                _invalidCredentials = failure;
                ValidateAllProperties();
                return;

            default:
                return;
        }
    }

    public static ValidationResult? ValidateMasterPassword(string? value, ValidationContext context)
    {
        UnlockPageViewModel vm = (UnlockPageViewModel)context.ObjectInstance;

        if (vm._invalidCredentials is null)
        {
            return ValidationResult.Success;
        }

        var error = new ValidationResult(vm._invalidCredentials.Reason);
        vm._invalidCredentials = null;
        vm.ClearErrors();

        return error;
    }
}
