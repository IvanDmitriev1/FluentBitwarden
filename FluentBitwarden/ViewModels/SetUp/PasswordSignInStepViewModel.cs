using BitwaredApi;
using BitwaredApi.Abstractions;
using BitwaredApi.Abstractions.Exceptions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Exceptions;

namespace FluentBitwarden.ViewModels.SetUp;

public sealed record SetupEnvironmentOption(string Title, string Subtitle, BitwardenEnvironment Environment);

public partial class PasswordSignInStepViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IEnvironmentConfig _environmentConfig;

    public PasswordSignInStepViewModel(
        SetupPageViewModel parentViewModel,
        IAuthService authService,
        IEnvironmentConfig environmentConfig)
    {
        ParentViewModel = parentViewModel;
        _authService = authService;
        _environmentConfig = environmentConfig;

        Environments =
        [
            new SetupEnvironmentOption("Bitwarden US", "bitwarden.com", BitwardenEnvironment.UnitedStates),
            new SetupEnvironmentOption("Bitwarden EU", "bitwarden.eu", BitwardenEnvironment.Europe),
        ];

        SelectedEnvironment = Environments[0];
    }

    public SetupPageViewModel ParentViewModel { get; }

    [ObservableProperty]
    public partial SetupEnvironmentOption? SelectedEnvironment { get; set; }

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MasterPassword { get; set; } = string.Empty;

    public string ServerHintText { get; } = "Select your environment and sign in.";

    public SetupEnvironmentOption[] Environments { get; }

    [RelayCommand]
    private void PasskeySignIn()
    {
        ParentViewModel.ShowError("Passkey sign-in is not implemented in this build.");
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task PasswordSignInAsync()
    {
        ParentViewModel.ClearStatus();

        if (SelectedEnvironment is null)
        {
            ParentViewModel.ShowError("Select a Bitwarden environment.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            ParentViewModel.ShowError("Enter your Bitwarden email address.");
            return;
        }

        if (string.IsNullOrWhiteSpace(MasterPassword))
        {
            ParentViewModel.ShowError("Enter your master password.");
            return;
        }

        _environmentConfig.Set(SelectedEnvironment.Environment);

        try
        {
            await ParentViewModel.RunBusyAsync(async () =>
            {
                await _authService.SignInWithPasswordAsync(Email.Trim(), MasterPassword);
                await ParentViewModel.CompleteAuthenticatedSessionAsync();
            });
        }
        catch (TwoFactorRequiredException ex)
        {
            ParentViewModel.EnterTwoFactorStep(ex.Challenge);
        }
        catch (Exception ex)
        {
            ParentViewModel.ShowError(ex);
        }
    }
}
