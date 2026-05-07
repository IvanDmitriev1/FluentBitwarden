using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Resources.Controls;
using FluentBitwarden.Views.Setup.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Modules.Session.Models;

namespace FluentBitwarden.Views.Setup.States;

public partial class PasswordSignInStepState(
    SetupLoginContext context,
    IAccountSessionManager accountSessionManager,
    Action<AccountSignInOutcome> onComplete) : ObservableValidator
{
    public string Email => context.Email;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [StringLength(int.MaxValue, MinimumLength = 8, ErrorMessage = "Master password must be at least 8 characters long.")]
    [Required(ErrorMessage = "Enter your master password.")]
    [CustomValidation(typeof(PasswordSignInStepState), nameof(ValidateMasterPassword))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "Generated setter delegates to ObservableValidator.ValidateProperty, which is intentionally preserved for this trim-aware validation path.")]
    public partial string MasterPassword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasInvalidCredentials { get; set; }

    [field: MaybeNull]
    public ValidatableProperty MasterPasswordValidation
        => field ??= ValidatableProperty.Create(this, static state => state.MasterPassword);

    public static ValidationResult? ValidateMasterPassword(string name, ValidationContext context)
    {
        PasswordSignInStepState instance = (PasswordSignInStepState)context.ObjectInstance;

        return !instance.HasInvalidCredentials
            ? ValidationResult.Success
            : new ValidationResult("Invalid master password");
    }

    partial void OnHasInvalidCredentialsChanged(bool value)
    {
        ValidateAllProperties();
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task SignInWithPasswordAsync()
    {
        HasInvalidCredentials = false;

        ValidateAllProperties();
        if (HasErrors)
        {
            return;
        }

        var result =
            await accountSessionManager.SignInAsync(
                new AccountSignInWithPasswordRequest(context.BitwardenClientContext, Email, MasterPassword),
                CancellationToken.None);

        switch (result)
        {
            case AccountSignInOutcome.Success:
                onComplete.Invoke(result);
                break;

            case AccountSignInOutcome.DeviceVerificationRequired:
            case AccountSignInOutcome.InvalidCredentials:
                HasInvalidCredentials = true;
                break;

            case AccountSignInOutcome.TwoFactorRequired:
                onComplete.Invoke(result);
                break;

            default:
                throw new InvalidOperationException("Unsupported password sign-in outcome.");
        }
    }
}
