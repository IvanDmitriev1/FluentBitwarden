using BitwardenApi.Shared.Context;
using FluentBitwarden.Modules.Account.Abstractions;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Security;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models.Authentication;
using FluentBitwarden.Shell.Navigation;
using FluentBitwarden.Views.Loading;
using FluentBitwarden.Views.Setup.Models;
using FluentBitwarden.Views.Setup.States;

namespace FluentBitwarden.Views.Setup;

public partial class SetupPageViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IAccountRepository _accountRepository;
    private readonly ISessionTokensStore _sessionTokensStore;
    private readonly SetupLoginContext _loginContext;

    public SetupPageViewModel(
        INavigationService navigationService,
        IAuthenticationService authenticationService,
        IAccountRepository accountRepository,
        ISessionTokensStore sessionTokensStore)
    {
        _navigationService = navigationService;
        _authenticationService = authenticationService;
        _accountRepository = accountRepository;
        _sessionTokensStore = sessionTokensStore;
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
        CurrentState = new PasswordSignInStepState(_loginContext, _authenticationService, GoTo2Fa);
    }

    private void GoTo2Fa(PasswordSignInOutcome.TwoFactorRequired twoFactorRequired)
    {
        CurrentState = new TwoFactorStepState(_loginContext, twoFactorRequired, _authenticationService, OnCompleteSetup);
    }

    private async Task OnCompleteSetup(AuthenticationSuccess success)
    {
        _sessionTokensStore.Store(success.UserId, success.SessionTokens);

        await _accountRepository.UpsertAsync(
            new StoredAccount(success.UserId, success.Email, _loginContext.DeviceInfoEnvironment,
                success.AccountCryptoMaterial, DateTimeOffset.UtcNow, false, false));

        _navigationService.NavigateTo<LoadingPage>();
    }
}
