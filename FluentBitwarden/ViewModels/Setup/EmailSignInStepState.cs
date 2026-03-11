using BitwaredApi;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Ui.Controls;
using System.ComponentModel.DataAnnotations;

namespace FluentBitwarden.ViewModels.Setup;

public sealed record SetupEnvironmentOption(string Title, string Subtitle, BitwardenEnvironment Environment);

public partial class EmailSignInStepState : ObservableValidator
{
    private readonly SetupPageViewModel _shell;

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
    public partial string Email { get; set; } = string.Empty;

    public SetupEnvironmentOption[] Environments { get; }

    public ValidatableProperty EmailValidation
        => field ??= ValidatableProperty.Create(this, static state => state.Email);

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
