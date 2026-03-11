using BitwaredApi;
using BitwaredApi.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Ui.Controls;
using System.ComponentModel.DataAnnotations;

namespace FluentBitwarden.ViewModels.Setup;

public sealed record SetupEnvironmentOption(string Title, string Subtitle, BitwardenEnvironment Environment);

public partial class EmailSignInStepState : ObservableValidator
{
    public EmailSignInStepState(SetupPageViewModel parentViewModel)
    {
        ParentViewModel = parentViewModel;

        Environments =
        [
            new SetupEnvironmentOption("Bitwarden US", "bitwarden.com", BitwardenEnvironment.UnitedStates),
            new SetupEnvironmentOption("Bitwarden EU", "bitwarden.eu", BitwardenEnvironment.Europe),
        ];

        SelectedEnvironment = Environments[0];
    }

    public SetupPageViewModel ParentViewModel { get; }

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


    [RelayCommand]
    private void Continue()
    {
        ValidateAllProperties();
        if (HasErrors)
            return;

        ParentViewModel.CurrentStep = SetupPageViewModel.SetupStep.PasswordSignIn;
    }
}
