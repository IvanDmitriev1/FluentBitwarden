using BitwardenApi.Shared.Context;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Resources.Controls;
using FluentBitwarden.Views.Setup.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.Views.Setup.States;

internal partial class EmailSignInStepState : ObservableValidator
{
    private readonly SetupLoginContext _context;
    private readonly Action _onContinue;

    public EmailSignInStepState(SetupLoginContext context, Action onContinue)
    {
        _context = context;
        _onContinue = onContinue;
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

    [field: AllowNull]
    public ValidatableProperty EmailValidation
        => field ??= ValidatableProperty.Create(this, static state => state.Email);

    [RelayCommand]
    private void Continue()
    {
        ValidateAllProperties();
        if (HasErrors)
        {
            return;
        }

        _context.Email = Email;
        _context.DeviceInfoEnvironment = SelectedEnvironment.Environment;

        _onContinue.Invoke();
    }
}
