using BitwardenApi.Client;
using BitwardenApi.Primitives.Ids;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.AppSession.Unlock;
using FluentBitwarden.Contracts.Infrastructure.WindowsHello;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Ui.Screens.Unlock;
using FluentBitwarden.Ui.Tests.TestInfrastructure;
using FluentBitwarden.Ui.Windows.Abstractions;
using NSubstitute;

namespace FluentBitwarden.Ui.Tests.Screens.Unlock;

[Trait("Category", "Unit")]
public sealed class UnlockViewModelTests
{
    private static readonly AccountProfile Account = new(
        UserId.Parse("11111111-1111-1111-1111-111111111111"),
        TestCredentials.Email,
        BitwardenEnvironment.Europe);
    private readonly IAppSessionClient _sessionClient = Substitute.For<IAppSessionClient>();
    private readonly IAccountWindowsHelloIntegrationClient _helloClient = Substitute.For<IAccountWindowsHelloIntegrationClient>();
    private readonly IWindowManager _windowManager = Substitute.For<IWindowManager>();
    private readonly UnlockPageViewModel _viewModel;

    public UnlockViewModelTests()
    {
        _helloClient.GetEnrollmentAsync(Arg.Any<GetWindowsHelloEnrollmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(WindowsHelloEnrollmentStatus.NotEnrolled));
        _viewModel = new UnlockPageViewModel(
            _sessionClient,
            _helloClient,
            _windowManager,
            Substitute.For<Microsoft.Extensions.Logging.ILogger<UnlockPageViewModel>>());
    }

    [Fact]
    public async Task BlankPasswordRequestsPasswordFocusWithoutIpc()
    {
        await NavigateToAccountAsync();

        await _viewModel.UnlockCommand.ExecuteAsync(" ");

        Assert.Equal(1, _viewModel.PasswordFocusRequest);
        Assert.False(_viewModel.IsUnlocking);
        await _sessionClient.DidNotReceive().UnlockAsync(Arg.Any<SessionUnlockRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PasswordUnlockSendsTypedRequestAndRejectionRestoresIdleStateAndFocus()
    {
        await NavigateToAccountAsync();
        var response = new TaskCompletionSource<SessionUnlockOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestStarted = new TaskCompletionSource<SessionUnlockRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
        _sessionClient.UnlockAsync(Arg.Any<SessionUnlockRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                requestStarted.TrySetResult(call.Arg<SessionUnlockRequest>());
                return response.Task;
            });

        Task unlock = _viewModel.UnlockCommand.ExecuteAsync("bad-password");
        SessionUnlockRequest request = await requestStarted.Task;
        Assert.True(_viewModel.IsUnlocking);
        Assert.Equal(
            new SessionUnlockRequest.MasterPasswordRequest(Account.UserId, "bad-password"),
            Assert.IsType<SessionUnlockRequest.MasterPasswordRequest>(request));

        response.SetResult(new SessionUnlockOutcome.Failure("Rejected."));
        await unlock;

        Assert.Equal("Rejected.", _viewModel.Error);
        Assert.Equal(1, _viewModel.PasswordFocusRequest);
        Assert.False(_viewModel.IsUnlocking);
        await _sessionClient.Received(1).UnlockAsync(
            Arg.Is<SessionUnlockRequest.MasterPasswordRequest>(typedRequest =>
                typedRequest.UserId == Account.UserId && typedRequest.MasterPassword == "bad-password"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnrolledWindowsHelloShowsButtonAndSendsTypedRequestWithWindowHandle()
    {
        const long handleValue = 2468;
        _helloClient.GetEnrollmentAsync(Arg.Any<GetWindowsHelloEnrollmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(WindowsHelloEnrollmentStatus.Enrolled));
        _windowManager.NativeWindowHandle.Returns(new NativeWindowHandle(handleValue));
        _sessionClient.UnlockAsync(Arg.Any<SessionUnlockRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SessionUnlockOutcome>(new SessionUnlockOutcome.Success()));

        await NavigateToAccountAsync();
        Assert.True(_viewModel.IsWindowHelloButtonVisible);
        _ = _helloClient.Received(1).GetEnrollmentAsync(
            Arg.Is<GetWindowsHelloEnrollmentRequest>(request => request.UserId == Account.UserId),
            Arg.Any<CancellationToken>());

        await _viewModel.UnlockWithWindowsHelloCommand.ExecuteAsync(null);

        await _sessionClient.Received(1).UnlockAsync(
            Arg.Is<SessionUnlockRequest.WindowsHelloRequest>(request =>
                request.UserId == Account.UserId && request.OwnerWindow == new NativeWindowHandle(handleValue)),
            Arg.Any<CancellationToken>());
        Assert.False(_viewModel.IsUnlocking);
    }

    [Fact]
    public async Task LateEnrollmentResultAfterLeavingPageDoesNotShowWindowsHello()
    {
        var response = new TaskCompletionSource<WindowsHelloEnrollmentStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestStarted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        _helloClient.GetEnrollmentAsync(Arg.Any<GetWindowsHelloEnrollmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                requestStarted.TrySetResult(call.Arg<CancellationToken>());
                return response.Task;
            });

        Task navigation = _viewModel.OnNavigatedToAsync(Account, CancellationToken.None).AsTask();
        CancellationToken statusToken = await requestStarted.Task;
        await _viewModel.OnNavigatedFromAsync(CancellationToken.None);
        response.SetResult(WindowsHelloEnrollmentStatus.Enrolled);
        await navigation;

        Assert.True(statusToken.IsCancellationRequested);
        Assert.False(_viewModel.IsWindowHelloButtonVisible);
    }

    private Task NavigateToAccountAsync() => _viewModel.OnNavigatedToAsync(Account, CancellationToken.None).AsTask();
}
