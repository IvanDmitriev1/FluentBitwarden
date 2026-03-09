using FluentBitwarden.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Ui.Controls;
using System.ComponentModel.DataAnnotations;

namespace FluentBitwarden.ViewModels;

public sealed partial class LoginUnlockMethodItem(
    LoginUnlockMethod method,
    string title,
    string description,
    bool hasSecretInput,
    string secretInputHeader,
    string unlockActionText,
    IRelayCommand unlockCommand)
    : ObservableValidator
{
    private ValidatableProperty? _secretInputValidation;

    public LoginUnlockMethod Method { get; } = method;
    public string Title { get; } = title;
    public string Description { get; } = description;
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public bool HasSecretInput { get; } = hasSecretInput;
    public string SecretInputHeader { get; } = secretInputHeader;
    public string UnlockActionText { get; } = unlockActionText;
    public IRelayCommand UnlockCommand { get; } = unlockCommand;

    [ObservableProperty]
    public partial bool IsAvailable { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(LoginUnlockMethodItem), nameof(ValidateSecretInput))]
    public partial string SecretInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool ShowValidationErrors { get; set; }

    public ValidatableProperty SecretInputValidation
        => _secretInputValidation ??= ValidatableProperty.Create(this, static method => method.SecretInput);

    public bool HasUnlockAction => IsAvailable;

    public void SetAvailability(bool isAvailable)
    {
        IsAvailable = isAvailable;
    }

    public void Reset()
    {
        SecretInput = string.Empty;
        ResetValidation();
    }

    public static ValidationResult? ValidateSecretInput(string? value, ValidationContext context)
    {
        LoginUnlockMethodItem method = (LoginUnlockMethodItem)context.ObjectInstance;

        if (!method.HasSecretInput || !string.IsNullOrWhiteSpace(value))
        {
            return ValidationResult.Success;
        }

        return method.Method switch
        {
            LoginUnlockMethod.Pin => new ValidationResult("Enter your app PIN."),
            LoginUnlockMethod.MasterPassword => new ValidationResult("Enter your master password."),
            _ => ValidationResult.Success,
        };
    }

    public bool TryValidateForSubmit()
    {
        ShowValidationErrors = true;
        ValidateAllProperties();
        return !HasErrors;
    }

    public void ResetValidation()
    {
        ShowValidationErrors = false;
    }
}
