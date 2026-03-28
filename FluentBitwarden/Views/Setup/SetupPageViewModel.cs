using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;
using FluentBitwarden.Modules.Security;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models.Authentication;
using FluentBitwarden.Views.Setup.Models;
using FluentBitwarden.Views.Setup.States;

namespace FluentBitwarden.Views.Setup;

public partial class SetupPageViewModel : ObservableObject
{
    private readonly IAuthenticationService _authenticationService;
    private readonly SetupLoginContext _loginContext;

    public SetupPageViewModel(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
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
        CurrentState = new TwoFactorStepState(_loginContext, twoFactorRequired, _authenticationService);
    }

    private void OnCompleteSetup()
    {

    }
}