using System.Diagnostics;
using BitwaredApi;
using BitwaredApi.Abstractions;
using BitwaredApi.Models.Auth;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Services;
using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.Views;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.ViewModels.Setup;

public partial class SetupPageViewModel : ObservableObject, IPageLifecycleAware
{
    public enum SetupStep
    {
        EmailSignIn,
        PasswordSignIn,
        TwoFactor,
    }

    private readonly IAuthenticationWorkflow _authenticationWorkflow;
    private readonly IVaultService _vaultService;
    private readonly LocalDeviceInfoProvider _deviceInfoProvider;
    private readonly INavigationService _navigationService;
    private readonly INotificationService _notificationService;

    private EmailSignInStepState _emailStep;
    private PasswordSignInStepState _passwordStep;
    private TwoFactorStepState _twoFactorStep;
    private PasswordSignInContinuation? _twoFactorContinuation;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentStepViewModel))]
    public partial SetupStep CurrentStep { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    public object CurrentStepViewModel { get; private set; }

    public SetupPageViewModel(
        IAuthenticationWorkflow authenticationWorkflow,
        IVaultService vaultService,
        IServiceProvider serviceProvider,
        INavigationService navigationService,
        INotificationService notificationService)
    {
        _authenticationWorkflow = authenticationWorkflow;
        _vaultService = vaultService;
        _deviceInfoProvider = serviceProvider.GetRequiredService<LocalDeviceInfoProvider>();
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
        if (value is not SetupStep.TwoFactor)
        {
            ClearTwoFactorContinuation();
        }

        switch (value)
        {
            case SetupStep.PasswordSignIn:
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

    public Task OnLoadingAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task OnUnloadingAsync(CancellationToken cancellationToken)
    {
        ClearTwoFactorContinuation();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void PasskeySignIn()
    {
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task SignInWithPasswordAsync()
    {
        IsBusy = true;
        _passwordStep.HasInvalidCredentials = false;

        try
        {
            BitwardenClientContext context = await GetClientContextAsync().ConfigureAwait(true);
            PasswordSignInOutcome signInOutcome = await _authenticationWorkflow.SignInWithPasswordAsync(
                new PasswordSignInRequest(
                    context,
                    _passwordStep.Email,
                    _passwordStep.MasterPassword)).ConfigureAwait(true);

            switch (signInOutcome)
            {
                case PasswordSignInOutcome.Success success:
                    await CompleteAuthenticatedSessionAsync(success.Authentication).ConfigureAwait(true);
                    break;

                case PasswordSignInOutcome.TwoFactorRequired twoFactorRequired:
                    ClearTwoFactorContinuation();
                    _twoFactorContinuation = twoFactorRequired.Continuation;
                    _twoFactorStep.Load(twoFactorRequired.Challenge);
                    CurrentStep = SetupStep.TwoFactor;
                    break;

                case PasswordSignInOutcome.InvalidCredentials:
                    _passwordStep.HasInvalidCredentials = true;
                    break;

                case PasswordSignInOutcome.DeviceVerificationRequired deviceVerificationRequired:
                    ShowError(deviceVerificationRequired.Message);
                    break;

                default:
                    throw new InvalidOperationException("Unsupported password sign-in outcome.");
            }
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

        if (_twoFactorContinuation is null)
        {
            ShowError("The two-factor session has expired. Sign in again.");
            CurrentStep = SetupStep.PasswordSignIn;
            return;
        }

        Debug.Assert(_twoFactorStep.SelectedProvider != null, "SelectedProvider should not be null if validation passed.");
        TwoFactorProviderOptionModel selectedProvider = _twoFactorStep.SelectedProvider;

        try
        {
            BitwardenClientContext context = await GetClientContextAsync().ConfigureAwait(true);
            AuthenticationOutcome authenticationOutcome = await _authenticationWorkflow.ContinueTwoFactorAsync(
                new TwoFactorSignInRequest(
                    context,
                    _twoFactorContinuation,
                    _twoFactorStep.Code.Trim(),
                    selectedProvider.Provider,
                    _twoFactorStep.RememberThisDevice)).ConfigureAwait(true);

            switch (authenticationOutcome)
            {
                case AuthenticationOutcome.Success success:
                    ClearTwoFactorContinuation();
                    await CompleteAuthenticatedSessionAsync(success.Authentication).ConfigureAwait(true);
                    break;

                case AuthenticationOutcome.InvalidCredentials invalidCredentials:
                    ShowError(invalidCredentials.Message);
                    break;

                case AuthenticationOutcome.DeviceVerificationRequired deviceVerificationRequired:
                    ShowError(deviceVerificationRequired.Message);
                    break;

                default:
                    throw new InvalidOperationException("Unsupported two-factor authentication outcome.");
            }
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task CompleteAuthenticatedSessionAsync(
        AuthenticationSuccess authentication,
        CancellationToken cancellationToken = default)
    {
        await _vaultService.AdoptAuthenticationAsync(authentication, cancellationToken).ConfigureAwait(true);
        await _vaultService.SyncAsync(cancellationToken).ConfigureAwait(true);
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

    private void ClearTwoFactorContinuation()
    {
        _twoFactorContinuation?.Dispose();
        _twoFactorContinuation = null;
    }

    private ValueTask<BitwardenClientContext> GetClientContextAsync(CancellationToken cancellationToken = default)
        => _deviceInfoProvider.GetClientContextAsync(
            _emailStep.SelectedEnvironment.Environment,
            cancellationToken);
}
