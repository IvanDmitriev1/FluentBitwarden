using FluentBitwarden.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FluentBitwarden.ViewModels;

public sealed partial class LoginUnlockMethodItem(
    LoginUnlockMethod method,
    string title,
    string description,
    bool hasSecretInput,
    string secretInputHeader,
    string unlockActionText,
    IRelayCommand unlockCommand)
    : ObservableObject
{
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
    public partial string SecretInput { get; set; } = string.Empty;

    public bool HasUnlockAction => IsAvailable;

    public void SetAvailability(bool isAvailable)
    {
        IsAvailable = isAvailable;
    }

    public void Reset()
    {
        SecretInput = string.Empty;
    }
}
