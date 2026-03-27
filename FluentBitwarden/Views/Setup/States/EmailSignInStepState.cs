using FluentBitwarden.Views.Setup.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using BitwardenApi.Context;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Resources.Controls;

namespace FluentBitwarden.Views.Setup.States;

internal partial class EmailSignInStepState : ObservableValidator
{
    public EmailSignInStepState()
    {
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

        
    }
}