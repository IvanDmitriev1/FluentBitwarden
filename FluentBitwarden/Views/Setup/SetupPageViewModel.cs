using BitwardenApi.Shared.Context;
using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Infrastructure.Services.Abstractions;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Security;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models.Authentication;
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
    private readonly IAuthenticationService _authenticationService;
    private readonly ISessionTokensStore _sessionTokensStore;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly IConnectivityService _connectivityService;
    private readonly SetupLoginContext _loginContext;

    public SetupPageViewModel(
        INavigationService navigationService,
        IAuthenticationService authenticationService,
        ISessionTokensStore sessionTokensStore,
        IUnitOfWorkFactory unitOfWorkFactory,
        IConnectivityService connectivityService)
    {
        _navigationService = navigationService;
        _authenticationService = authenticationService;
        _sessionTokensStore = sessionTokensStore;
        _unitOfWorkFactory = unitOfWorkFactory;
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
            _authenticationService,
            GoTo2Fa);
    }

    private void GoTo2Fa(PasswordSignInOutcome.TwoFactorRequired twoFactorRequired)
    {
        CurrentState = new TwoFactorStepState(
            _loginContext,
            twoFactorRequired,
            _authenticationService,
            OnCompleteSetup);
    }

    private void OnCompleteSetup(AuthenticationSuccess success)
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        unitOfWork.AccountRepository.Upsert(new StoredAccount(
            success.UserId,
            success.Email,
            _loginContext.DeviceInfoEnvironment,
            LastSyncAt: DateTimeOffset.MinValue));

        unitOfWork.AccountDecryptionRepository.Upsert(success.AccountDecryption);

        _sessionTokensStore.Store(success.UserId, success.SessionTokens);
        unitOfWork.SaveChanges();

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
