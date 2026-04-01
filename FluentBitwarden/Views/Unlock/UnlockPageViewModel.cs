using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Resources.Controls;
using FluentBitwarden.Shared.Behaviors.Lifecycle;
using FluentBitwarden.Views.Unlock.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Modules.Security.Abstractions;
using FluentBitwarden.Views.Loading;
using FluentBitwarden.Modules.Security.Models.Unlock;
using FluentBitwarden.Modules.Security.Services.Unlock;
using FluentBitwarden.Views.Shell.Navigation;

namespace FluentBitwarden.Views.Unlock;

public sealed partial class UnlockPageViewModel(
    IUnlockService unlockService,
    INavigationService navigationService) : ObservableValidator, IPageLifecycleAware<IReadOnlyList<StoredAccount>>
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
    public async Task OnLoadingAsync(IReadOnlyList<StoredAccount> param, CancellationToken cancellationToken)
    {
        SelectedAccount = param[0];

        var capabilities = await unlockService.GetCapabilitiesAsync(SelectedAccount.UserId, cancellationToken);
        UnlockMethods = CreateUnlockOptions(capabilities);
    }

    public void OnUnloading() { }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task UnlockMasterPassword()
    {
        var result = await unlockService.UnlockAsync(SelectedAccount!.UserId, new MasterPasswordUnlockRequest(Password));
        if (result is UnlockResult.Success {} unlockResult)
        {
            navigationService.NavigateTo<LoadingPage>(PageNavigationParameter.From(unlockResult));
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

    private static IReadOnlyList<UnlockOption> CreateUnlockOptions(in UnlockCapabilities capabilities)
    {
        int size = 1 + Convert.ToInt32(capabilities.SupportsPin) + Convert.ToInt32(capabilities.SupportsWindowsHello);

        var methods = new List<UnlockOption>(size);
        methods.Add(new UnlockOption(UnlockMethod.MasterPassword, "Master password"));

        if (capabilities.SupportsPin)
        {
            methods.Add(new UnlockOption(UnlockMethod.Pin, "Pin"));
        }

        if (capabilities.SupportsWindowsHello)
        {
            methods.Add(new UnlockOption(UnlockMethod.WindowsHello, "Windows Hello"));
        }

        return methods;
    }
}
