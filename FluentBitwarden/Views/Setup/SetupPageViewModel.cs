using BitwardenApi.Shared.Context;
using FluentBitwarden.Infrastructure.Security;
using FluentBitwarden.Infrastructure.Services.Abstractions;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models;
using FluentBitwarden.Resources.Controls.Lifecycle;
using FluentBitwarden.Views.Loading;
using FluentBitwarden.Views.Offline;
using FluentBitwarden.Views.Offline.Models;
using FluentBitwarden.Views.Setup.Models;
using FluentBitwarden.Views.Setup.States;

namespace FluentBitwarden.Views.Setup;

public partial class SetupPageViewModel : ObservableObject, IPageLifecycleAware
{
    private readonly INavigationService _navigationService;
    private readonly IAccountSessionManager _accountSessionManager;
    private readonly IConnectivityService _connectivityService;
    private readonly SetupLoginContext _loginContext;

    public SetupPageViewModel(
        INavigationService navigationService,
        IAccountSessionManager accountSessionManager,
        IConnectivityService connectivityService)
    {
        _navigationService = navigationService;
        _accountSessionManager = accountSessionManager;
        _connectivityService = connectivityService;
        _loginContext = new SetupLoginContext(DeviceIdentity.DeviceInfo, BitwardenEnvironment.UnitedStates);

        GoToEmail();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    public partial object CurrentState { get; private set; } = null!;

    public bool CanGoBack => CurrentState is not EmailSignInStepState;

    private void GoToEmail()
    {
        CurrentState = new EmailSignInStepState(_loginContext, GoToPasswordSignIn);
    }

    private void GoToPasswordSignIn()
    {
        CurrentState = new PasswordSignInStepState(
            _loginContext,
            _accountSessionManager,
            OnPasswordSignIn);
    }

    private void OnPasswordSignIn(AccountSignInOutcome outcome)
    {
        switch (outcome)
        {
            case AccountSignInOutcome.Success:
                OnCompleteSetup();
                return;
            case AccountSignInOutcome.TwoFactorRequired twoFactorRequired:
                CurrentState = new TwoFactorStepState(
                    _loginContext,
                    twoFactorRequired,
                    _accountSessionManager,
                    OnCompleteSetup);
                return;
            default:
                throw new InvalidOperationException("Unsupported password sign-in outcome.");
        }
    }

    private void OnCompleteSetup()
    {
        _navigationService.NavigateTo<LoadingPage>();
    }

    public Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        _connectivityService.ConnectivityChanged += ConnectivityServiceOnConnectivityChanged;

        return Task.CompletedTask;
    }

    public void OnUnloading()
    {
        _connectivityService.ConnectivityChanged -= ConnectivityServiceOnConnectivityChanged;
    }

    private void ConnectivityServiceOnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (e.HasInternetAccess)
            return;

        _navigationService.NavigateTo<OfflinePage>(
            PageNavigationParameter.From(new OfflinePageParameter(OfflinePageReason.FirstSignInRequiresInternet)));
    }
}
