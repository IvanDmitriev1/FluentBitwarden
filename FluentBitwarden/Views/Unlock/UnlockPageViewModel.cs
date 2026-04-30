using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Modules.Account.Models;
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
using System.Linq;
using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Modules.Vault.Models;
using FluentBitwarden.Modules.Vault.Abstractions;
using FluentBitwarden.Shared.Services.Abstractions;

namespace FluentBitwarden.Views.Unlock;

public sealed partial class UnlockPageViewModel(
    IUnlockService unlockService,
    INavigationService navigationService,
    IVaultSyncService vaultSyncService,
    IConnectivityService connectivityService,
    ISiteIconCache siteIconCache) : ObservableValidator, IPageLifecycleAware<UnlockPageParameter>
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
            case UnlockResult.Success success:
                OnSuccessUnlock(success.userKey);
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

    private void OnSuccessUnlock(DecryptedUserKey decryptedUserKey)
    {
        vaultSyncService.LoadAllFromDb(decryptedUserKey);

        if (connectivityService.HasInternetAccess)
        {
            var urls = vaultSyncService.Ciphers
                .OfType<LoginCipher>()
                .Select(static c => c.Uris.FirstOrDefault())
                .Where(static s => !string.IsNullOrWhiteSpace(s))
                .Select(static s => Uri.TryCreate(s, UriKind.Absolute, out var uri) ? uri : null)
                .Where(static uri => uri is not null)
                .Cast<Uri>()
                .ToArray();

            _ = Task.Run(() => siteIconCache.PreloadAsync(urls, CancellationToken.None));
        }
        
        navigationService.NavigateTo<ShellPage>();
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
