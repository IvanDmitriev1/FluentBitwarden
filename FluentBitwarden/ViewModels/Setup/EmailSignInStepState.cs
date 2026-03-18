using BitwaredApi;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Ui.Controls;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.ViewModels.Setup;

public sealed record SetupEnvironmentOption(string Title, string Subtitle, BitwardenEnvironment Environment);

[UnconditionalSuppressMessage(
    "Trimming",
    "IL2026",
    Justification = "ObservableValidator validation uses reflection-based metadata and the generated helper is preserved explicitly.")]
public partial class EmailSignInStepState : ObservableValidator
{
    private readonly SetupPageViewModel _shell;

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "ObservableValidator constructs ValidationContext via reflection for this validation-only step model.")]
    internal EmailSignInStepState(SetupPageViewModel shell)
    {
        _shell = shell;

        Environments =
        [
            new SetupEnvironmentOption("Bitwarden US", "bitwarden.com", BitwardenEnvironment.UnitedStates),
            new SetupEnvironmentOption("Bitwarden EU", "bitwarden.eu", BitwardenEnvironment.Europe),
        ];

        SelectedEnvironment = Environments[0];
    }

    [ObservableProperty]
    public partial SetupEnvironmentOption SelectedEnvironment { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Required(ErrorMessage = "Enter your Bitwarden email address.")]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "Generated setter delegates to ObservableValidator.ValidateProperty, which is intentionally preserved for this trim-aware validation path.")]
    public partial string Email { get; set; } = string.Empty;

    public SetupEnvironmentOption[] Environments { get; }

    [field: AllowNull, MaybeNull]
    public ValidatableProperty EmailValidation
        => field ??= ValidatableProperty.Create(this, static state => state.Email);

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "This method populates a validation-bound property through the generated setter intentionally.")]
    public void OnActivated()
    {
        if (!string.IsNullOrWhiteSpace(_shell.FlowContext.Email))
        {
            Email = _shell.FlowContext.Email;
        }

        SelectedEnvironment = Array.Find(
            Environments,
            option => option.Environment == _shell.FlowContext.ClientContext.Environment) ?? Environments[0];
    }

    [RelayCommand]
    private void PasskeySignIn()
    {
    }

    [RelayCommand]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "This method intentionally validates all properties and normalizes the validation-bound email through its generated setter.")]
    private void Continue()
    {
        ValidateAllProperties();
        if (HasErrors)
        {
            return;
        }

        Email = Email.Trim();
        _shell.FlowContext.Email = Email;
        _shell.FlowContext.ChangeEnvironment(SelectedEnvironment.Environment);
        _shell.ShowPasswordStep();
    }
}
