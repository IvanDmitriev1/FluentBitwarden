using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Ui.Controls;
using System.ComponentModel.DataAnnotations;
using static FluentBitwarden.ViewModels.Setup.SetupPageViewModel;

namespace FluentBitwarden.ViewModels.Setup;

public partial class PasswordSignInStepState(SetupPageViewModel parentViewModel) : ObservableValidator
{
    public SetupPageViewModel ParentViewModel { get; } = parentViewModel;

    [ObservableProperty]
    public partial string Email { get; private set; } = string.Empty;

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


    public void Load(string email)
    {
        Email = email;
        MasterPassword = string.Empty;
        HasInvalidCredentials = false;

        ClearErrors();
    }

    [RelayCommand]
    private void BackToEmail()
    {
        ParentViewModel.CurrentStep = SetupStep.EmailSignIn;
    }

    partial void OnHasInvalidCredentialsChanged(bool value)
    {
        ValidateAllProperties();
    }

    public static ValidationResult? ValidateMasterPassword(string name, ValidationContext context)
    {
        var instance = (PasswordSignInStepState)context.ObjectInstance;

        return !instance.HasInvalidCredentials
            ? ValidationResult.Success
            : new ValidationResult("Invalid master password");
    }

}
