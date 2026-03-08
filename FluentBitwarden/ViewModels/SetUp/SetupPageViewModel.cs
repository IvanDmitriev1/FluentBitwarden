using BitwaredApi.Abstractions;
using BitwaredApi.Abstractions.Exceptions;
using BitwaredApi.Models.Auth;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.Views;

namespace FluentBitwarden.ViewModels.SetUp;

public partial class SetupPageViewModel : ObservableObject
{
    public enum SetupStep
    {
        PasswordSignIn,
        TwoFactor,
    }

    private readonly IAuthService _authService;
    private readonly IVaultService _vaultService;
    private readonly IEnvironmentConfig _environmentConfig;
    private readonly INavigationService _navigationService;
    private readonly PasswordSignInStepViewModel _passwordStepViewModel;
    private readonly TwoFactorStepViewModel _twoFactorStepViewModel;

    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial SetupStep CurrentStep { get; set; } = SetupStep.PasswordSignIn;


    public object? CurrentStepViewModel => CurrentStep switch
    {
        SetupStep.TwoFactor => _twoFactorStepViewModel,
        _ => _passwordStepViewModel,
    };

    public SetupPageViewModel(
        IAuthService authService,
        IVaultService vaultService,
        IEnvironmentConfig environmentConfig,
        INavigationService navigationService)
    {
        _authService = authService;
        _vaultService = vaultService;
        _environmentConfig = environmentConfig;
        _navigationService = navigationService;

        _passwordStepViewModel = new PasswordSignInStepViewModel(this);
        _twoFactorStepViewModel = new TwoFactorStepViewModel(this);
    }

    partial void OnCurrentStepChanged(SetupStep value)
    {
        OnPropertyChanged(nameof(CurrentStepViewModel));
    }

    [RelayCommand]
    private void PasskeySignIn()
    {
        ShowError("Passkey sign-in is not implemented in this build.");
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task PasswordSignInAsync()
    {
        ClearError();
        PasswordSignInStepViewModel passwordStep = _passwordStepViewModel;

        if (passwordStep.SelectedEnvironment is null)
        {
            ShowError("Select a Bitwarden environment.");
            return;
        }

        if (string.IsNullOrWhiteSpace(passwordStep.Email))
        {
            ShowError("Enter your Bitwarden email address.");
            return;
        }

        if (string.IsNullOrWhiteSpace(passwordStep.MasterPassword))
        {
            ShowError("Enter your master password.");
            return;
        }

        _environmentConfig.Set(passwordStep.SelectedEnvironment.Environment);
        IsBusy = true;

        try
        {
            await _authService.SignInWithPasswordAsync(passwordStep.Email.Trim(), passwordStep.MasterPassword);
            ResetTwoFactorState();
            await _vaultService.SyncAsync();
            _navigationService.Navigate(typeof(VaultPage), clearBackStack: true);
        }
        catch (TwoFactorRequiredException ex)
        {
            EnterTwoFactorStep(ex.Challenge);
        }
        catch (InvalidCredentialsException ex)
        {
            ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ContinueTwoFactorAsync()
    {
        ClearError();
        TwoFactorStepViewModel twoFactorStep = _twoFactorStepViewModel;

        if (twoFactorStep.SelectedProvider is null)
        {
            ShowError("Select a two-factor provider.");
            return;
        }

        if (!twoFactorStep.SelectedProvider.IsSupported)
        {
            ShowError("This provider requires an interactive Bitwarden flow that is not implemented in this build.");
            return;
        }

        if (string.IsNullOrWhiteSpace(twoFactorStep.Code))
        {
            ShowError("Enter the verification code.");
            return;
        }

        IsBusy = true;

        try
        {
            await _authService.ContinueTwoFactorAsync(
                twoFactorStep.Code.Trim(),
                twoFactorStep.SelectedProvider.Provider,
                twoFactorStep.RememberThisDevice);
            ResetTwoFactorState();
            await _vaultService.SyncAsync();
            _navigationService.Navigate(typeof(VaultPage), clearBackStack: true);
        }
        catch (InvalidCredentialsException ex)
        {
            ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void BackFromTwoFactor()
    {
        ClearError();
        ResetTwoFactorState();
    }

    private void EnterTwoFactorStep(TwoFactorChallenge challenge)
    {
        _twoFactorStepViewModel.LoadChallenge(challenge);
        CurrentStep = SetupStep.TwoFactor;
    }

    private void ResetTwoFactorState()
    {
        _twoFactorStepViewModel.Reset();
        CurrentStep = SetupStep.PasswordSignIn;
    }

    private void ClearError()
    {
        HasError = false;
        ErrorMessage = string.Empty;
    }

    private void ShowError(string message)
    {
        HasError = true;
        ErrorMessage = message;
    }
}
