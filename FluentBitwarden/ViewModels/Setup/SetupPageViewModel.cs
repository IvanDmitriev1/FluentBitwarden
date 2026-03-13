using BitwaredApi.Abstractions;
using BitwaredApi.Models.Auth;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.Views;
using FluentBitwarden.Views.Vault;

namespace FluentBitwarden.ViewModels.Setup;

public partial class SetupPageViewModel : ObservableObject, IPageLifecycleAware
{
    private readonly IVaultService _vaultService;
    private readonly INavigationService _navigationService;
    private readonly INotificationService _notificationService;
    private readonly EmailSignInStepState _emailStep;
    private readonly PasswordSignInStepState _passwordStep;
    private readonly TwoFactorStepState _twoFactorStep;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial object CurrentStepViewModel { get; private set; }

    public SetupFlowContext FlowContext { get; }

    public SetupPageViewModel(
        IAuthenticationWorkflow authenticationWorkflow,
        IVaultService vaultService,
        ILocalDeviceInfoProvider deviceInfoProvider,
        INavigationService navigationService,
        INotificationService notificationService)
    {
        _vaultService = vaultService;
        _navigationService = navigationService;
        _notificationService = notificationService;
        FlowContext = new SetupFlowContext(deviceInfoProvider.DeviceInfo);

        _twoFactorStep = new TwoFactorStepState(this, authenticationWorkflow);
        _passwordStep = new PasswordSignInStepState(this, authenticationWorkflow);
        _emailStep = new EmailSignInStepState(this);
        CurrentStepViewModel = _emailStep;

        ShowEmailStep();
    }

    public Task OnLoadingAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task OnUnloadingAsync()
    {
        IsBusy = false;
        _twoFactorStep.Dispose();
        return Task.CompletedTask;
    }

    public void ShowEmailStep()
    {
        CurrentStepViewModel = _emailStep;
        _emailStep.OnActivated();
    }

    public void ShowPasswordStep()
    {
        CurrentStepViewModel = _passwordStep;
        _passwordStep.OnActivated();
    }

    public void ShowTwoFactorSignIn(PasswordSignInOutcome.TwoFactorRequired outcome)
    {
        _twoFactorStep.Begin(outcome);
        CurrentStepViewModel = _twoFactorStep;
    }

    public async Task CompleteAuthenticatedSessionAsync(
        AuthenticationSuccess authentication,
        CancellationToken cancellationToken = default)
    {
        await _vaultService.AdoptAuthenticationAsync(authentication, cancellationToken);
        await _vaultService.SyncAsync(cancellationToken);
        _navigationService.Navigate<ShellPage>(clearBackStack: true);
    }

    public void ShowError(string message)
    {
        _notificationService.ShowError("Sign in failed", message);
    }

    public void ShowError(Exception exception)
    {
        ShowError(exception.Message);
    }
}
