using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FluentBitwarden.ViewModels;

public sealed record SetupEnvironmentOption(string Title, string Subtitle);

public partial class SetupPageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial SetupEnvironmentOption? SelectedEnvironment { get; set; }

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public string MasterPassword { get; set; } = string.Empty;

    public string ServerHintText { get; } = "Select your environment and sign in.";

    public SetupEnvironmentOption[] Environments { get; }

    public SetupPageViewModel()
    {
        Environments =
        [
            new SetupEnvironmentOption("Bitwarden US", "bitwarden.com"),
            new SetupEnvironmentOption("Bitwarden EU", "bitwarden.eu"),
        ];

        SelectedEnvironment = Environments[0];
    }


    [RelayCommand]
    private void PasskeySignIn()
    {
        ClearError();
    }

    [RelayCommand]
    private void PasswordSignIn()
    {
        ClearError();
    }
    private void ClearError()
    {
        HasError = false;
        ErrorMessage = string.Empty;
    }
}
