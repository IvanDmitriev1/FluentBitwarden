using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Models;
using FluentBitwarden.Models.Vault;
using FluentBitwarden.Ui.Controls;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.ViewModels.Login;

[UnconditionalSuppressMessage(
    "Trimming",
    "IL2026",
    Justification = "ObservableValidator validation uses reflection-based metadata. Custom validators and generated validation helpers are preserved explicitly.")]
public sealed partial class MasterPasswordUnlockViewModel : ObservableValidator, ILoginUnlockMethod
{
    private readonly LoginPageViewModel _parentViewModel;
    private readonly IVaultService _vaultService;
    private VaultUnlockOutcome.InvalidCredentials? _invalidCredentials;

    [DynamicDependency(nameof(ValidateMasterPassword), typeof(MasterPasswordUnlockViewModel))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "ObservableValidator constructs ValidationContext via reflection. This viewmodel keeps its custom validator method explicitly preserved.")]
    public MasterPasswordUnlockViewModel(LoginPageViewModel parentViewModel, IVaultService vaultService)
    {
        _parentViewModel = parentViewModel;
        _vaultService = vaultService;
    }

    public string Title => "Unlock with master password";
    public string Description => string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Enter your master password.")]
    [CustomValidation(typeof(MasterPasswordUnlockViewModel), nameof(ValidateMasterPassword))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "Generated setter delegates to ObservableValidator.ValidateProperty, which is intentionally preserved for this trim-aware validation path.")]
    public partial string MasterPassword { get; set; } = string.Empty;

    public ValidatableProperty MasterPasswordValidation
        => field ??= ValidatableProperty.Create(this, static state => state.MasterPassword);

    [RelayCommand(AllowConcurrentExecutions = false)]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "Full property validation is intentionally used here and the generated validator helper is preserved.")]
    private async Task Unlock()
    {
        var outcome = await _vaultService.UnlockAsync(MasterPassword);
        if (outcome is VaultUnlockOutcome.InvalidCredentials invalidCredentials)
        {
            _invalidCredentials = invalidCredentials;
            ValidateAllProperties();
            return;
        }

        _parentViewModel.HandleUnlockOutcomeAsync(outcome);
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
