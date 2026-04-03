using BitwardenApi.Shared.Context;
using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Security;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models.Authentication;
using FluentBitwarden.Views.Loading;
using FluentBitwarden.Views.Setup.Models;
using FluentBitwarden.Views.Setup.States;
using FluentBitwarden.Views.Shell.Navigation;

namespace FluentBitwarden.Views.Setup;

public partial class SetupPageViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IAuthenticationService _authenticationService;
    private readonly ISessionTokensStore _sessionTokensStore;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly SetupLoginContext _loginContext;

    public SetupPageViewModel(
        INavigationService navigationService,
        IAuthenticationService authenticationService,
        ISessionTokensStore sessionTokensStore,
        IUnitOfWorkFactory unitOfWorkFactory)
    {
        _navigationService = navigationService;
        _authenticationService = authenticationService;
        _sessionTokensStore = sessionTokensStore;
        _unitOfWorkFactory = unitOfWorkFactory;
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
        using var unitOfWork = _unitOfWorkFactory.Create();

        await Task.Run(() =>
        {
            unitOfWork.AccountRepository.Upsert(new StoredAccount(
                success.UserId,
                success.Email,
                _loginContext.DeviceInfoEnvironment,
                LastSyncAt: DateTimeOffset.MinValue));

            unitOfWork.AccountDecryptionRepository.Upsert(success.AccountDecryption);
        });

        _sessionTokensStore.Store(success.UserId, success.SessionTokens);
        unitOfWork.SaveChanges();

        _navigationService.NavigateTo<LoadingPage>();
    }
}
