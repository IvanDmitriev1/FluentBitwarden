using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Resources;
using FluentBitwarden.UI.Controls;
using FluentBitwarden.Views.Accounts.LogIn.Models;
using FluentBitwarden.Views.Accounts.LogIn.ValidationAttributes;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Contracts.Modules.Accounts.Login;
using FluentBitwarden.Infrastructure.Abstractions;
using FluentBitwarden.Infrastructure.Extensions;
using WinUIEx;

namespace FluentBitwarden.Views.Accounts.LogIn.States;

internal sealed partial class LogInEmailStepViewModel : ObservableValidatorEx
{
    public LogInEmailStepViewModel(
        LogInFlowPageViewModel flow,
        IWindowManager windowManager)
    {
        _flow = flow;
        _windowManager = windowManager;

        Environments =
        [
            LogInEnvironmentOption.Us,
            LogInEnvironmentOption.Eu,
            new LogInEnvironmentOption("Custom", string.Empty),
        ];

        SelectedEnvironment = Environments[0];
    }

    private readonly LogInFlowPageViewModel _flow;
    private readonly IWindowManager _windowManager;

    public LogInEnvironmentOption[] Environments { get; }

    [ObservableProperty]
    public partial string? PasskeyErrorMessage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomEnvironmentSelected))]
    public partial LogInEnvironmentOption? SelectedEnvironment { get; set; }

    public bool IsCustomEnvironmentSelected => SelectedEnvironment?.Title == "Custom";


    [ObservableProperty]
    [NotifyDataErrorInfo]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Required(ErrorMessage = "Enter your Bitwarden email address.")]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification =
            "Generated setter delegates to ObservableValidator.ValidateProperty, which is intentionally preserved for this trim-aware validation path.")]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomServerUrl<LogInEmailStepViewModel>(shouldValidateMemberName:nameof(IsCustomEnvironmentSelected))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "Generated setter delegates to ObservableValidator.ValidateProperty, which is intentionally preserved for this trim-aware validation path.")]
    public partial string CustomServerUrl { get; set; } = string.Empty;




    [field: AllowNull]
    public ValidatableProperty EmailValidation
        => field ??= ValidatableProperty.Create(this,
            static x => x.Email);

    [field: AllowNull]
    public ValidatableProperty CustomServerUrlValidation
        => field ??= ValidatableProperty.Create(this,
            static x => x.CustomServerUrl);


    [RelayCommand]
    private void Continue()
    {
        ValidateAllProperties();
        if (HasErrors || SelectedEnvironment is null)
            return;

        _flow.Context.Email = Email;
        _flow.Context.ChangeEnvironment(SelectedEnvironment.Value.ToBitwardenEnvironment(CustomServerUrl));
        _flow.ShowPasswordStep();
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task LogInWithPasskeyAsync()
    {
        PasskeyErrorMessage = null;
        ClearErrors(nameof(Email));
        ValidateProperty(CustomServerUrl, nameof(CustomServerUrl));

        if (SelectedEnvironment is null || HasValidationErrors(nameof(CustomServerUrl)))
            return;

        _flow.Context.Email = Email.Trim();
        _flow.Context.ChangeEnvironment(SelectedEnvironment.Value.ToBitwardenEnvironment(CustomServerUrl));

        ArgumentNullException.ThrowIfNull(_windowManager.ActiveWindow);

        var outcome = await _flow.AccountsClient.LoginAsync(
            new AccountLoginRequest.PasskeyRequest(_flow.Context.BitwardenContext, _windowManager.ActiveWindow.GetWindowHandle()), CancellationToken.None);

        switch (outcome)
        {
            case AccountLoginOutcome.Success:
                _flow.OnSuccessLogIn();
                return;

            case AccountLoginOutcome.TwoFactorRequired:
                PasskeyErrorMessage = "This account requires two-step verification. Sign in with your master password instead.";
                return;

            case AccountLoginOutcome.DeviceVerificationRequired deviceVerificationRequired:
                PasskeyErrorMessage = deviceVerificationRequired.Message;
                return;

            case AccountLoginOutcome.InvalidCredentials invalidCredentials:
                PasskeyErrorMessage = invalidCredentials.Message;
                return;

            default:
                throw new InvalidOperationException("Unsupported passkey sign-in outcome.");
        }
    }

    private bool HasValidationErrors(string propertyName) =>
        GetErrors(propertyName).Cast<object>().Any();
}
