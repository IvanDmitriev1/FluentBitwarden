using BitwardenApi.Client;
using BitwardenApi.Identity.Models;
using BitwardenApi.Primitives.Ids;
using BitwardenApi.Vault.Ciphers.Models;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.AppSession.Unlock;
using FluentBitwarden.Contracts.Infrastructure.WindowsHello;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.Authentication;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;
using FluentBitwarden.Contracts.Settings;
using FluentBitwarden.Platform.Ipc.Abstractions;
using FluentBitwarden.Platform.SiteIcons;
using FluentBitwarden.Ui.Infrastructure;
using FluentBitwarden.Ui.Infrastructure.Hosting.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using NSubstitute;

namespace FluentBitwarden.Ui.Tests;

internal sealed class UiTestComposition
{
    public const string Rejection = "UI test sign-in rejected.";
    public const string AuthorizationHash = "synthetic-ui-test-authorization-hash";

    public static readonly AccountProfile Account = new(
        UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        TestInfrastructure.TestCredentials.Email,
        new BitwardenEnvironment(
            new Uri("https://api.ui-test.example.test"),
            new Uri("https://identity.ui-test.example.test"),
            new Uri("https://notifications.ui-test.example.test"),
            new Uri("https://vault.ui-test.example.test")));

    private readonly IAppSessionClient _appSessionClient = Substitute.For<IAppSessionClient>();
    private readonly IAccountClient _accountClient = Substitute.For<IAccountClient>();
    private readonly IIpcEventClient _eventClient = Substitute.For<IIpcEventClient>();
    private readonly IAccountWindowsHelloIntegrationClient _windowsHelloClient = Substitute.For<IAccountWindowsHelloIntegrationClient>();
    private readonly IVaultCipherClient _vaultCipherClient = Substitute.For<IVaultCipherClient>();
    private AsyncIpcEventCallback<AppSessionStateChangedEvent>? _sessionChangedCallback;
    private Func<AccountAuthenticationRequest, AccountAuthenticationOutcome> _authenticationOutcome = DefaultAuthenticationOutcome;
    private string? _unlockRejection;
    private AppSessionStateSnapshot _sessionState = new AppSessionStateSnapshot.NotAuthenticated();
    private TaskCompletionSource<SessionUnlockRequest> _unlockRequest = NewUnlockRequestSource();

    public IAppSessionClient AppSessionClient => _appSessionClient;
    public IVaultCipherClient VaultCipherClient => _vaultCipherClient;

    public void AddTestServices(IServiceCollection services)
    {
        _appSessionClient.GetStateAsync(Arg.Any<GetAppSessionStateRequest>(), Arg.Any<CancellationToken>())
            .Returns(GetSessionStateAsync);
        _appSessionClient.UnlockAsync(Arg.Any<SessionUnlockRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _unlockRequest.TrySetResult(call.Arg<SessionUnlockRequest>());
                return Task.FromResult<SessionUnlockOutcome>(_unlockRejection is { } rejection
                    ? new SessionUnlockOutcome.Failure(rejection)
                    : new SessionUnlockOutcome.Success());
            });
        _accountClient.GetAccountsAsync(Arg.Any<GetAccountsRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Array.Empty<AccountProfile>()));
        _vaultCipherClient.SearchCiphersAsync(Arg.Any<VaultCipherQuery>(), Arg.Any<CancellationToken>())
            .Returns(SearchCiphersAsync);
        _accountClient.AuthenticateAsync(Arg.Any<AccountAuthenticationRequest>(), Arg.Any<CancellationToken>())
            .Returns(AuthenticateAsync);
        _eventClient.Subscribe<AppSessionStateChangedEvent>(Arg.Do<AsyncIpcEventCallback<AppSessionStateChangedEvent>>(
                callback => _sessionChangedCallback = callback))
            .Returns(Substitute.For<IDisposable>());
        _windowsHelloClient.GetEnrollmentAsync(Arg.Any<GetWindowsHelloEnrollmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(WindowsHelloEnrollmentStatus.NotEnrolled));

        var settingsStore = Substitute.For<ISettingsStore>();
        settingsStore.Get(UiSettingKeys.Appearance.ThemeKey).Returns(ElementTheme.Default);

        services.AddSingleton<App>();
        services.AddSingleton<IActivationHandler>(static sp => sp.GetRequiredService<App>());
        services.AddSingleton(_appSessionClient);
        services.AddSingleton(_accountClient);
        services.AddSingleton(_eventClient);
        services.AddSingleton(_windowsHelloClient);
        services.AddSingleton(_vaultCipherClient);
        services.AddSingleton(Substitute.For<ISiteIconCache>());
        services.AddSingleton(Substitute.For<IIpcExceptionNotifier>());
        services.AddSingleton(settingsStore);
    }

    public void Reset()
    {
        _sessionState = new AppSessionStateSnapshot.NotAuthenticated();
        _authenticationOutcome = DefaultAuthenticationOutcome;
        _unlockRejection = null;
        _unlockRequest = NewUnlockRequestSource();
        _appSessionClient.GetStateAsync(Arg.Any<GetAppSessionStateRequest>(), Arg.Any<CancellationToken>())
            .Returns(GetSessionStateAsync);
        _vaultCipherClient.SearchCiphersAsync(Arg.Any<VaultCipherQuery>(), Arg.Any<CancellationToken>())
            .Returns(SearchCiphersAsync);
        _appSessionClient.ClearReceivedCalls();
        _accountClient.ClearReceivedCalls();
        _windowsHelloClient.ClearReceivedCalls();
        _vaultCipherClient.ClearReceivedCalls();
    }

    public void ConfigureAuthenticatorRejection()
    {
        var challenge = new IdentityTwoFactorChallenge(
            (IdentityTwoFactorProviderOption[])[
                new IdentityTwoFactorProviderOption(IdentityTwoFactorProviderType.Authenticator)]);
        _authenticationOutcome = request => request switch
        {
            AccountAuthenticationRequest.Password => new AccountAuthenticationOutcome.TwoFactorRequired(
                challenge,
                TestInfrastructure.TestCredentials.Email,
                AuthorizationHash),
            AccountAuthenticationRequest.TwoFactor => new AccountAuthenticationOutcome.InvalidCredentials(Rejection),
            _ => new AccountAuthenticationOutcome.InvalidCredentials("Unexpected test authentication request."),
        };
    }

    public void ConfigureUnlockRejection(string reason) => _unlockRejection = reason;

    public async Task PublishSessionStateAsync(AppSessionStateSnapshot state, CancellationToken cancellationToken)
    {
        _sessionState = state;
        AsyncIpcEventCallback<AppSessionStateChangedEvent> callback = _sessionChangedCallback
            ?? throw new InvalidOperationException("The app session monitor has not subscribed to session events.");
        await callback(new AppSessionStateChangedEvent(state), cancellationToken);
    }

    public Task<SessionUnlockRequest> WaitForUnlockRequestAsync(CancellationToken cancellationToken) =>
        _unlockRequest.Task.WaitAsync(cancellationToken);

    public static AccountProfile CreateAccount(string? email = null) =>
        Account with { Email = email ?? TestInfrastructure.TestCredentials.Email };

    private Task<AppSessionStateSnapshot> GetSessionStateAsync(NSubstitute.Core.CallInfo _) => Task.FromResult(_sessionState);

    private Task<Cipher[]> SearchCiphersAsync(NSubstitute.Core.CallInfo _) => Task.FromResult(Array.Empty<Cipher>());

    private Task<AccountAuthenticationOutcome> AuthenticateAsync(NSubstitute.Core.CallInfo call) =>
        Task.FromResult(_authenticationOutcome(call.Arg<AccountAuthenticationRequest>()));

    private static AccountAuthenticationOutcome DefaultAuthenticationOutcome(AccountAuthenticationRequest request) =>
        request is AccountAuthenticationRequest.Password
        {
            Email: TestInfrastructure.TestCredentials.Email,
            MasterPassword: TestInfrastructure.TestCredentials.Password
        }
            ? new AccountAuthenticationOutcome.InvalidCredentials(Rejection)
            : new AccountAuthenticationOutcome.InvalidCredentials("Unexpected test credentials.");

    private static TaskCompletionSource<SessionUnlockRequest> NewUnlockRequestSource() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
