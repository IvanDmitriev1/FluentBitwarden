using BitwardenApi.Client;
using BitwardenApi.Identity.Models;
using BitwardenApi.Primitives.Ids;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.Authentication;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Ui.Application.Abstractions;
using FluentBitwarden.Ui.Screens.Setup;
using FluentBitwarden.Ui.Screens.Setup.Steps.Email;
using FluentBitwarden.Ui.Screens.Setup.Steps.TwoFactor;
using FluentBitwarden.Ui.Tests.TestInfrastructure;
using NSubstitute;

namespace FluentBitwarden.Ui.Tests.Screens.Setup;

[Trait("Category", "Unit")]
public sealed class SetupTwoFactorViewModelTests
{
    private readonly IAccountClient _accountClient = Substitute.For<IAccountClient>();
    private readonly ISetupNavigator _navigator = Substitute.For<ISetupNavigator>();
    private readonly IAppSession _sessionRefresher = Substitute.For<IAppSession>();
    private readonly SetupTwoFactorPageViewModel _viewModel;

    public SetupTwoFactorViewModelTests()
    {
        _viewModel = new SetupTwoFactorPageViewModel(_accountClient, _navigator, _sessionRefresher);
    }

    [Fact]
    public async Task MixedProvidersExposeSupportedMethodAndReportUnsupportedMethods()
    {
        await NavigateToChallengeAsync(IdentityTwoFactorProviderType.Authenticator, IdentityTwoFactorProviderType.Duo);

        Assert.Equal(new[] { IdentityTwoFactorProviderType.Authenticator }, _viewModel.SupportedProviders.Select(provider => provider.Provider));
        Assert.Equal("Duo", _viewModel.UnsupportedProvidersText);
        Assert.Equal("Methods unavailable in this build: Duo", _viewModel.UnsupportedProvidersMessage);
        Assert.True(_viewModel.HasUnsupportedProviders);
        Assert.Equal(IdentityTwoFactorProviderType.Authenticator, _viewModel.SelectedProvider);
    }

    [Fact]
    public async Task UnsupportedOnlyProvidersCannotBeSubmitted()
    {
        await NavigateToChallengeAsync(IdentityTwoFactorProviderType.Duo);
        _viewModel.Code = "123456";

        Assert.Empty(_viewModel.SupportedProviders);
        Assert.Null(_viewModel.SelectedProviderOption);
        Assert.False(_viewModel.CanSubmit);
        Assert.Equal("This account only has verification methods that are not supported in this build.", _viewModel.Error);
        Assert.Equal("Duo", _viewModel.UnsupportedProvidersText);
    }

    [Fact]
    public async Task BlankCodeCannotSubmitAndDoesNotCallIpc()
    {
        await NavigateToChallengeAsync(IdentityTwoFactorProviderType.Authenticator);

        Assert.False(_viewModel.CanSubmit);
        await _viewModel.SubmitCommand.ExecuteAsync(null);

        await _accountClient.DidNotReceive().AuthenticateAsync(
            Arg.Any<AccountAuthenticationRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidProofRejectionShowsErrorAndClearsCode()
    {
        await NavigateToChallengeAsync(IdentityTwoFactorProviderType.Email);
        _viewModel.Code = "123456";
        _accountClient.AuthenticateAsync(Arg.Any<AccountAuthenticationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AccountAuthenticationOutcome>(new AccountAuthenticationOutcome.InvalidCredentials("Invalid code.")));

        await _viewModel.SubmitCommand.ExecuteAsync(null);

        Assert.Equal("Invalid code.", _viewModel.Error);
        Assert.Equal(string.Empty, _viewModel.Code);
        Assert.False(_viewModel.IsBusy);
        await _accountClient.Received(1).AuthenticateAsync(
            Arg.Is<AccountAuthenticationRequest>(request =>
                request.GetType() == typeof(AccountAuthenticationRequest.TwoFactor) &&
                ((AccountAuthenticationRequest.TwoFactor)request).Email == TestCredentials.Email &&
                ((AccountAuthenticationRequest.TwoFactor)request).ServerAuthorizationHash == "authorization-hash" &&
                ((AccountAuthenticationRequest.TwoFactor)request).TwoFactorProof.Code == "123456" &&
                ((AccountAuthenticationRequest.TwoFactor)request).TwoFactorProof.Provider == IdentityTwoFactorProviderType.Email),
            Arg.Any<CancellationToken>());
        await _sessionRefresher.DidNotReceive().RefreshSessionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SuccessfulProofClearsCodeAndRefreshesSession()
    {
        await NavigateToChallengeAsync(IdentityTwoFactorProviderType.Authenticator);
        _viewModel.Code = "654321";
        _accountClient.AuthenticateAsync(Arg.Any<AccountAuthenticationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AccountAuthenticationOutcome>(new AccountAuthenticationOutcome.Success(CreateAccount())));

        await _viewModel.SubmitCommand.ExecuteAsync(null);

        Assert.Equal(string.Empty, _viewModel.Code);
        Assert.Null(_viewModel.Error);
        Assert.False(_viewModel.IsBusy);
        await _accountClient.Received(1).AuthenticateAsync(
            Arg.Is<AccountAuthenticationRequest>(request =>
                request.GetType() == typeof(AccountAuthenticationRequest.TwoFactor) &&
                ((AccountAuthenticationRequest.TwoFactor)request).TwoFactorProof.Code == "654321" &&
                ((AccountAuthenticationRequest.TwoFactor)request).TwoFactorProof.Provider == IdentityTwoFactorProviderType.Authenticator),
            Arg.Any<CancellationToken>());
        await _sessionRefresher.Received(1).RefreshSessionAsync(Arg.Any<CancellationToken>());

        _viewModel.Code = "123456";
        await _viewModel.SubmitCommand.ExecuteAsync(null);
        Assert.Equal("The verification challenge has expired. Go back and try again.", _viewModel.Error);
        await _accountClient.Received(1).AuthenticateAsync(
            Arg.Any<AccountAuthenticationRequest>(), Arg.Any<CancellationToken>());
    }

    private Task NavigateToChallengeAsync(params IdentityTwoFactorProviderType[] providers)
    {
        var challenge = new AccountAuthenticationOutcome.TwoFactorRequired(
            new IdentityTwoFactorChallenge(providers.Select(static provider => new IdentityTwoFactorProviderOption(provider)).ToArray()),
            TestCredentials.Email,
            "authorization-hash");
        return _viewModel.OnNavigatedToAsync(
            new SetupTwoFactorParameters(
                new BitwardenClientContext(
                    BitwardenEnvironment.Europe,
                    new DeviceInfo(DeviceIdentifier.Parse("ui-test-device"), DeviceName.Parse("UI test"))),
                challenge,
                new SetupEmailParameters(TestCredentials.Email, SetupServerOption.Eu, string.Empty)),
            CancellationToken.None).AsTask();
    }

    private static AccountProfile CreateAccount() => new(
        UserId.Parse("11111111-1111-1111-1111-111111111111"),
        TestCredentials.Email,
        BitwardenEnvironment.Europe);
}
