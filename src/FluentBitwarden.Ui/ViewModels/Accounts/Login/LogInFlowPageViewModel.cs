using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Application.Abstractions;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Infrastructure.Window;
using FluentBitwarden.Application.Models;
using FluentBitwarden.Contracts.Modules.Accounts.Authentication;
using FluentBitwarden.Views.Accounts;
using FluentBitwarden.Views.Shell;
using FluentBitwarden.Views.Startup;

namespace FluentBitwarden.ViewModels.Accounts.Login;

public sealed partial class LogInFlowPageViewModel : ObservableObject
{
    public LogInFlowPageViewModel(
        IAccountClient accountClient,
        IWindowManager windowManager,
        IAppCoordinator appCoordinator,
        INavigation navigation)
    {
        _windowManager = windowManager;
        _appCoordinator = appCoordinator;
        _navigation = navigation;
        AccountClient = accountClient;
        CurrentStep = new LogInEmailStepViewModel(this, _windowManager);
    }

    private readonly IWindowManager _windowManager;
    private readonly IAppCoordinator _appCoordinator;
    private readonly INavigation _navigation;

    internal IAccountClient AccountClient { get; }
    internal LogInFlowContext Context { get; } = new();


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    public partial object CurrentStep { get; private set; }

    public bool CanGoBack => CurrentStep is not LogInEmailStepViewModel;

    internal void ShowEmailStep()
    {
        CurrentStep = new LogInEmailStepViewModel(this, _windowManager);
    }

    internal void ShowPasswordStep()
    {
        CurrentStep = new LogInPasswordStepViewModel(this);
    }

    internal void Show2FStep(AccountAuthenticationOutcome.TwoFactorRequired twoFactorRequired)
    {
        CurrentStep = new LogIn2FStepViewModel(twoFactorRequired, this);
    }

    internal Task OnSuccessLogIn(AccountProfile account) => _appCoordinator.RefreshSessionAsync();

    public ValueTask OnNavigatedToAsync(CancellationToken cancellationToken)
    {
        _appCoordinator.SessionStateApplied += OnSessionStateApplied;
        return ValueTask.CompletedTask;
    }

    public ValueTask OnNavigatedFromAsync(CancellationToken cancellationToken)
    {
        _appCoordinator.SessionStateApplied -= OnSessionStateApplied;
        return ValueTask.CompletedTask;
    }

    private void OnSessionStateApplied(
        AppSessionState state,
        UnlockPageParameter? unlockParameter,
        OpenVaultCipherIntent? openIntent)
    {
        switch (state)
        {
            case AppSessionState.Locked when unlockParameter is not null:
                _navigation.Root.Navigate<UnlockPage, UnlockPageParameter>(
                    unlockParameter,
                    NavigationKind.Reset);
                break;
            case AppSessionState.Unlocked when _windowManager.ActiveMode == WindowMode.Main:
                if (openIntent is null)
                {
                    _navigation.Root.Navigate<ShellPage>(NavigationKind.Reset);
                }
                else
                {
                    _navigation.Root.Navigate<ShellPage, OpenVaultCipherIntent>(
                        openIntent,
                        NavigationKind.Reset);
                }

                break;
            case AppSessionState.Unlocked:
                _navigation.Root.Navigate<LoadingPage>(NavigationKind.Reset);
                break;
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        ShowEmailStep();
    }
}
