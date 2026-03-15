using BitwaredApi;
using BitwaredApi.Abstractions;
using BitwaredApi.Models.Auth;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Ui.Controls;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.ViewModels.Setup;

[UnconditionalSuppressMessage(
    "Trimming",
    "IL2026",
    Justification = "ObservableValidator validation uses reflection-based metadata. Custom validators and generated validation helpers are preserved explicitly.")]
public partial class PasswordSignInStepState : ObservableValidator
{
    private readonly SetupPageViewModel _shell;
    private readonly IAuthenticationWorkflow _authenticationWorkflow;

    [DynamicDependency(nameof(ValidateMasterPassword), typeof(PasswordSignInStepState))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "ObservableValidator constructs ValidationContext via reflection. This viewmodel keeps its custom validator method explicitly preserved.")]
    internal PasswordSignInStepState(
        SetupPageViewModel shell,
        IAuthenticationWorkflow authenticationWorkflow)
    {
        _shell = shell;
        _authenticationWorkflow = authenticationWorkflow;
    }

    public string Email => _shell.FlowContext.Email;

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

    public ValidatableProperty MasterPasswordValidation
        => field ??= ValidatableProperty.Create(this, static state => state.MasterPassword);

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "Resetting the validation-bound password uses the generated setter intentionally.")]
    public void OnActivated()
    {
        OnPropertyChanged(nameof(Email));
        MasterPassword = string.Empty;
        HasInvalidCredentials = false;
        ClearErrors();
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "Full property validation is intentionally used here and the generated validator helper is preserved.")]
    private async Task SignInWithPasswordAsync()
    {
        ValidateAllProperties();
        if (HasErrors)
        {
            return;
        }

        BitwardenClientContext context = _shell.FlowContext.ClientContext;

        HasInvalidCredentials = false;

        try
        {
            _shell.IsBusy = true;

            PasswordSignInOutcome signInOutcome = await _authenticationWorkflow
                .SignInWithPasswordAsync(
                    new PasswordSignInRequest(
                        context,
                        Email,
                        MasterPassword))
                .ConfigureAwait(true);

            switch (signInOutcome)
            {
                case PasswordSignInOutcome.Success success:
                    await _shell.CompleteAuthenticatedSessionAsync(success.Authentication).ConfigureAwait(true);
                    break;

                case PasswordSignInOutcome.TwoFactorRequired twoFactorRequired:
                    _shell.ShowTwoFactorSignIn(twoFactorRequired);
                    break;

                case PasswordSignInOutcome.InvalidCredentials:
                    HasInvalidCredentials = true;
                    break;

                case PasswordSignInOutcome.DeviceVerificationRequired deviceVerificationRequired:
                    _shell.ShowError(deviceVerificationRequired.Message);
                    break;

                default:
                    throw new InvalidOperationException("Unsupported password sign-in outcome.");
            }
        }
        catch (Exception ex)
        {
            _shell.ShowError(ex);
        }
        finally
        {
            _shell.IsBusy = false;
        }
    }

    [RelayCommand]
    private void BackToEmail()
    {
        _shell.ShowEmailStep();
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "Changing the invalid-credentials state triggers a full validation refresh intentionally.")]
    partial void OnHasInvalidCredentialsChanged(bool value)
    {
        ValidateAllProperties();
    }

    public static ValidationResult? ValidateMasterPassword(string name, ValidationContext context)
    {
        PasswordSignInStepState instance = (PasswordSignInStepState)context.ObjectInstance;

        return !instance.HasInvalidCredentials
            ? ValidationResult.Success
            : new ValidationResult("Invalid master password");
    }
}
