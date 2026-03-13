using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Models;
using FluentBitwarden.Models.Vault;
using FluentBitwarden.Ui.Controls;
using System.ComponentModel.DataAnnotations;

namespace FluentBitwarden.ViewModels.Login;

public sealed partial class MasterPasswordUnlockViewModel(LoginPageViewModel parentViewModel, IVaultService vaultService) : ObservableValidator, ILoginUnlockMethod
{
    public string Title => "Unlock with master password";
    public string Description => string.Empty;

    private VaultUnlockOutcome.InvalidCredentials? _invalidCredentials;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Enter your master password.")]
    [CustomValidation(typeof(MasterPasswordUnlockViewModel), nameof(ValidateMasterPassword))]
    public partial string MasterPassword { get; set; } = string.Empty;

    public ValidatableProperty MasterPasswordValidation
        => field ??= ValidatableProperty.Create(this, static state => state.MasterPassword);

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task Unlock()
    {
        var outcome = await vaultService.UnlockAsync(MasterPassword);
        if (outcome is VaultUnlockOutcome.InvalidCredentials invalidCredentials)
        {
            _invalidCredentials = invalidCredentials;
            ValidateAllProperties();
            return;
        }

        parentViewModel.HandleUnlockOutcomeAsync(outcome);
    }

    public static ValidationResult? ValidateMasterPassword(string? value, ValidationContext context)
    {
        MasterPasswordUnlockViewModel vm = (MasterPasswordUnlockViewModel)context.ObjectInstance;

        if (vm._invalidCredentials is null)
        {
            return ValidationResult.Success;
        }

        var error = new ValidationResult(vm._invalidCredentials.Message);
        vm._invalidCredentials = null;
        vm.ClearErrors();

        return error;
    }
}