using BitwaredApi;
using BitwaredApi.Abstractions;
using BitwaredApi.Models.Auth;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Ui.Controls;
using System.ComponentModel.DataAnnotations;

namespace FluentBitwarden.ViewModels.Setup;

public partial class PasswordSignInStepState : ObservableValidator
{
    private readonly SetupPageViewModel _shell;
    private readonly IAuthenticationWorkflow _authenticationWorkflow;

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
    [MinLength(8, ErrorMessage = "Master password must be at least 8 characters long.")]
    [Required(ErrorMessage = "Enter your master password.")]
    [CustomValidation(typeof(PasswordSignInStepState), nameof(ValidateMasterPassword))]
    public partial string MasterPassword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasInvalidCredentials { get; set; }

    public ValidatableProperty MasterPasswordValidation
        => field ??= ValidatableProperty.Create(this, static state => state.MasterPassword);

    public void OnActivated()
    {
        OnPropertyChanged(nameof(Email));
        MasterPassword = string.Empty;
        HasInvalidCredentials = false;
        ClearErrors();
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
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
