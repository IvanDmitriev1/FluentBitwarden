using BitwardenApi.Client;
using BitwardenApi.Identity.Models;
using BitwardenApi.Primitives.Ids;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.Authentication;
using FluentBitwarden.Ui.Application.Abstractions;
using FluentBitwarden.Ui.Screens.Setup;
using FluentBitwarden.Ui.Screens.Setup.Steps.Email;
using FluentBitwarden.Ui.Screens.Setup.Steps.Password;
using FluentBitwarden.Ui.Tests.TestInfrastructure;
using NSubstitute;

namespace FluentBitwarden.Ui.Tests.Screens.Setup;

[Trait("Category", "Unit")]
public sealed class SetupPasswordViewModelTests
{
    private readonly IAccountClient _accountClient = Substitute.For<IAccountClient>();
    private readonly ISetupNavigator _navigator = Substitute.For<ISetupNavigator>();
    private readonly IAppSession _sessionRefresher = Substitute.For<IAppSession>();
    private readonly SetupPasswordPageViewModel _viewModel;

    public SetupPasswordViewModelTests()
    {
        _viewModel = new SetupPasswordPageViewModel(_accountClient, _navigator, _sessionRefresher);
    }

    [Fact]
    public async Task BlankPasswordDoesNotCallIpcAndRequestsPasswordReset()
    {
        await NavigateToPasswordAsync();

        await _viewModel.LoginCommand.ExecuteAsync("  ");

        Assert.Equal("Enter your master password.", _viewModel.Error);
        Assert.Equal(2, _viewModel.PasswordResetRequest);
        await _accountClient.DidNotReceive().AuthenticateAsync(
            Arg.Any<AccountAuthenticationRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectedPasswordShowsErrorResetsInputAndSendsAccountArguments()
    {
        await NavigateToPasswordAsync();
        _accountClient.AuthenticateAsync(Arg.Any<AccountAuthenticationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AccountAuthenticationOutcome>(new AccountAuthenticationOutcome.InvalidCredentials("Rejected.")));

        await _viewModel.LoginCommand.ExecuteAsync("entered-password");

        Assert.Equal("Rejected.", _viewModel.Error);
        Assert.False(_viewModel.IsBusy);
        Assert.Equal(2, _viewModel.PasswordResetRequest);
        await _accountClient.Received(1).AuthenticateAsync(
            Arg.Is<AccountAuthenticationRequest>(request =>
                request.GetType() == typeof(AccountAuthenticationRequest.Password) &&
                ((AccountAuthenticationRequest.Password)request).Email == TestCredentials.Email &&
                ((AccountAuthenticationRequest.Password)request).MasterPassword == "entered-password" &&
                ((AccountAuthenticationRequest.Password)request).Context == CreateContext()),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SuccessfulPasswordLoginRefreshesSessionAndResetsInput()
    {
        await NavigateToPasswordAsync();
        _accountClient.AuthenticateAsync(Arg.Any<AccountAuthenticationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AccountAuthenticationOutcome>(new AccountAuthenticationOutcome.Success(CreateAccount())));

        await _viewModel.LoginCommand.ExecuteAsync("entered-password");

        Assert.Null(_viewModel.Error);
        Assert.False(_viewModel.IsBusy);
        Assert.Equal(2, _viewModel.PasswordResetRequest);
        await _sessionRefresher.Received(1).RefreshSessionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TwoFactorLoginNavigatesWithTheChallengeAndEmailContext()
    {
        SetupEmailParameters emailState = CreateEmailState();
        BitwardenClientContext context = CreateContext();
        var challenge = new AccountAuthenticationOutcome.TwoFactorRequired(
            new IdentityTwoFactorChallenge([new IdentityTwoFactorProviderOption(IdentityTwoFactorProviderType.Email)]),
            TestCredentials.Email,
            "authorization-hash");
        await _viewModel.OnNavigatedToAsync(
            new SetupPasswordParameters(TestCredentials.Email, context, SetupServerOption.Eu.Title, emailState),
            CancellationToken.None);
        _accountClient.AuthenticateAsync(Arg.Any<AccountAuthenticationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AccountAuthenticationOutcome>(challenge));

        await _viewModel.LoginCommand.ExecuteAsync("entered-password");

        _navigator.Received(1).ShowTwoFactor(Arg.Is<SetupTwoFactorParameters>(parameters =>
            parameters.Context == context &&
            parameters.Challenge == challenge &&
            parameters.EmailState == emailState));
        Assert.Equal(2, _viewModel.PasswordResetRequest);
    }

    [Fact]
    public async Task LeavingPageCancelsLoginAndIgnoresLateRejection()
    {
        await NavigateToPasswordAsync();
        var response = new TaskCompletionSource<AccountAuthenticationOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestStarted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        _accountClient.AuthenticateAsync(Arg.Any<AccountAuthenticationRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                requestStarted.TrySetResult(call.Arg<CancellationToken>());
                return response.Task;
            });

        Task login = _viewModel.LoginCommand.ExecuteAsync("entered-password");
        CancellationToken requestToken = await requestStarted.Task;
        await _viewModel.OnNavigatedFromAsync(CancellationToken.None);
        response.SetResult(new AccountAuthenticationOutcome.InvalidCredentials("Late rejection."));
        await login;

        Assert.True(requestToken.IsCancellationRequested);
        Assert.Null(_viewModel.Error);
        Assert.False(_viewModel.IsBusy);
        Assert.Equal(3, _viewModel.PasswordResetRequest);
    }

    private Task NavigateToPasswordAsync() => _viewModel.OnNavigatedToAsync(
        new SetupPasswordParameters(TestCredentials.Email, CreateContext(), SetupServerOption.Eu.Title, CreateEmailState()),
        CancellationToken.None).AsTask();

    private static BitwardenClientContext CreateContext() => new(
        BitwardenEnvironment.Europe,
        new DeviceInfo(DeviceIdentifier.Parse("ui-test-device"), DeviceName.Parse("UI test")));

    private static SetupEmailParameters CreateEmailState() =>
        new(TestCredentials.Email, SetupServerOption.Eu, string.Empty);

    private static FluentBitwarden.Contracts.Modules.Accounts.StoredAccount.AccountProfile CreateAccount() => new(
        UserId.Parse("11111111-1111-1111-1111-111111111111"),
        TestCredentials.Email,
        BitwardenEnvironment.Europe);
}
