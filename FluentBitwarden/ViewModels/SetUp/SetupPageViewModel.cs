using System.Diagnostics;
using BitwaredApi.Abstractions;
using BitwaredApi.Abstractions.Exceptions;
using BitwaredApi.Models.Auth;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Exceptions;
using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.Views;

namespace FluentBitwarden.ViewModels.SetUp;

public partial class SetupPageViewModel : ObservableObject
{
    public enum SetupStep
    {
        EmailSignIn,
        PasswordSignIn,
        TwoFactor,
    }

    private readonly IAuthService _authService;
    private readonly IEnvironmentConfig _environmentConfig;
    private readonly INavigationService _navigationService;
    private readonly INotificationService _notificationService;

    private EmailSignInStepState _emailStep;
    private PasswordSignInStepState _passwordStep;
    private TwoFactorStepState _twoFactorStep;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentStepViewModel))]
    public partial SetupStep CurrentStep { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    public object CurrentStepViewModel { get; private set; }

    public SetupPageViewModel(
        IAuthService authService,
        IEnvironmentConfig environmentConfig,
        INavigationService navigationService,
        INotificationService notificationService)
    {
        _authService = authService;
        _environmentConfig = environmentConfig;
        _navigationService = navigationService;
        _notificationService = notificationService;

        _emailStep = new EmailSignInStepState(this);
        _passwordStep = new PasswordSignInStepState(this);
        _twoFactorStep = new TwoFactorStepState(this);

        CurrentStep = SetupStep.EmailSignIn;
        CurrentStepViewModel = _emailStep;
    }

    partial void OnCurrentStepChanged(SetupStep value)
    {
        switch (value)
        {
            case SetupStep.EmailSignIn:
                _authService.CancelPendingAuthFlow();
                break;
            case SetupStep.PasswordSignIn:
                _environmentConfig.Set(_emailStep.SelectedEnvironment.Environment);
                _passwordStep.Load(_emailStep.Email);
                break;
        }

        CurrentStepViewModel = value switch
        {
            SetupStep.EmailSignIn => _emailStep,
            SetupStep.PasswordSignIn => _passwordStep,
            SetupStep.TwoFactor => _twoFactorStep,
            _ => throw new InvalidOperationException("Invalid setup step.")
        };
    }

    [RelayCommand]
    private void PasskeySignIn()
    {
        // Intentionally left empty until passkey sign-in is implemented.
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task SignInWithPasswordAsync()
    {
        IsBusy = true;

        try
        {
            await _authService.SignInWithPasswordAsync(
                _passwordStep.Email,
                _passwordStep.MasterPassword);

            CompleteAuthenticatedSession();
        }
        catch (TwoFactorRequiredException ex)
        {
            _twoFactorStep.Load(ex.Challenge);
            CurrentStep = SetupStep.TwoFactor;
        }
        catch (InvalidCredentialsException ex)
        {
            _passwordStep.HasInvalidCredentials = true;
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ContinueTwoFactorAsync()
    {
        if (!_twoFactorStep.TryValidateForSubmit())
        {
            return;
        }

        Debug.Assert(_twoFactorStep.SelectedProvider != null, "SelectedProvider should not be null if validation passed.");
        TwoFactorProviderOptionModel selectedProvider = _twoFactorStep.SelectedProvider;

        try
        {
            await _authService.ContinueTwoFactorAsync(
                _twoFactorStep.Code.Trim(),
                selectedProvider.Provider,
                _twoFactorStep.RememberThisDevice).ConfigureAwait(true);

            CompleteAuthenticatedSession();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    public void CompleteAuthenticatedSession()
    {
        _navigationService.Navigate<VaultPage>(clearBackStack: true);
    }

    public void ShowError(string message)
    {
        _notificationService.ShowError("Sign in failed", message);
    }

    public void ShowError(Exception exception)
    {
        ShowError(AuthErrorMessageFormatter.Format(exception));
    }
}
