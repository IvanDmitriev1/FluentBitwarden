using BitwardenApi.Client;
using BitwardenApi.Primitives.Ids;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.Infrastructure.WindowsHello;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Ui.Features.Settings.Models;
using FluentBitwarden.Ui.Tests.TestInfrastructure;
using FluentBitwarden.Ui.Windows.Abstractions;
using NSubstitute;

namespace FluentBitwarden.Ui.Tests.Features.Settings;

[Trait("Category", "Unit")]
public sealed class WindowsHelloSettingValueTests
{
    private static readonly AccountProfile Account = new(
        UserId.Parse("11111111-1111-1111-1111-111111111111"),
        TestCredentials.Email,
        BitwardenEnvironment.Europe);

    private readonly IAppSessionClient _sessionClient = Substitute.For<IAppSessionClient>();
    private readonly IAccountWindowsHelloIntegrationClient _helloClient = Substitute.For<IAccountWindowsHelloIntegrationClient>();
    private readonly IWindowManager _windowManager = Substitute.For<IWindowManager>();
    private readonly WindowsHelloSettingValue _value;

    public WindowsHelloSettingValueTests()
    {
        _value = new WindowsHelloSettingValue(_sessionClient, _helloClient, _windowManager);
    }

    [Theory]
    [InlineData(WindowsHelloEnrollmentStatus.Unavailable, false, false)]
    [InlineData(WindowsHelloEnrollmentStatus.NotEnrolled, true, false)]
    [InlineData(WindowsHelloEnrollmentStatus.Enrolled, true, true)]
    public async Task LoadMapsActiveAccountEnrollmentStatus(
        WindowsHelloEnrollmentStatus status,
        bool expectedSupported,
        bool expectedEnabled)
    {
        _sessionClient.GetStateAsync(Arg.Any<GetAppSessionStateRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AppSessionStateSnapshot>(new AppSessionStateSnapshot.Locked(Account)));
        _helloClient.GetEnrollmentAsync(Arg.Any<GetWindowsHelloEnrollmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(status));

        await _value.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(expectedSupported, _value.IsSupported);
        Assert.Equal(expectedEnabled, _value.IsEnabled);
        _ = _helloClient.Received(1).GetEnrollmentAsync(
            Arg.Is<GetWindowsHelloEnrollmentRequest>(request => request.UserId == Account.UserId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadUsesEnrollmentWhenSignedInAndClearsItWhenSignedOut()
    {
        _sessionClient.GetStateAsync(Arg.Any<GetAppSessionStateRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AppSessionStateSnapshot>(new AppSessionStateSnapshot.Unlocked(Account)));
        _helloClient.GetEnrollmentAsync(Arg.Any<GetWindowsHelloEnrollmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(WindowsHelloEnrollmentStatus.Enrolled));

        await _value.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(_value.IsSupported);
        Assert.True(_value.IsEnabled);
        _ = _helloClient.Received(1).GetEnrollmentAsync(
            Arg.Is<GetWindowsHelloEnrollmentRequest>(request => request.UserId == Account.UserId),
            Arg.Any<CancellationToken>());

        _sessionClient.GetStateAsync(Arg.Any<GetAppSessionStateRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AppSessionStateSnapshot>(new AppSessionStateSnapshot.NotAuthenticated()));
        _helloClient.ClearReceivedCalls();
        await _value.LoadAsync(TestContext.Current.CancellationToken);

        Assert.False(_value.IsSupported);
        Assert.False(_value.IsEnabled);
        _ = _helloClient.DidNotReceive().EnableAsync(Arg.Any<EnableWindowsHelloEnrollmentRequest>(), Arg.Any<CancellationToken>());
        _ = _helloClient.DidNotReceive().DisableAsync(Arg.Any<DisableWindowsHelloEnrollmentRequest>(), Arg.Any<CancellationToken>());
        _ = _helloClient.DidNotReceive().GetEnrollmentAsync(
            Arg.Any<GetWindowsHelloEnrollmentRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnablingSendsNativeWindowHandleAndKeepsEnrolledState()
    {
        const long handle = 2468;
        var started = WindowsHelloSettingValueTestSupport.NewSource<EnableWindowsHelloEnrollmentRequest>();
        await LoadActiveAccountAsync(WindowsHelloEnrollmentStatus.NotEnrolled);
        _windowManager.NativeWindowHandle.Returns(new NativeWindowHandle(handle));
        _helloClient.EnableAsync(Arg.Any<EnableWindowsHelloEnrollmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                started.TrySetResult(call.Arg<EnableWindowsHelloEnrollmentRequest>());
                return Task.FromResult(WindowsHelloEnrollmentOutcome.Enrolled);
            });

        _value.IsEnabled = true;

        EnableWindowsHelloEnrollmentRequest request = await started.Task;
        Assert.Equal(new NativeWindowHandle(handle), request.OwnerWindow);
        Assert.True(_value.IsEnabled);
        _ = _helloClient.Received(1).EnableAsync(
            Arg.Any<EnableWindowsHelloEnrollmentRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectedEnrollmentRestoresDisabledState()
    {
        await LoadActiveAccountAsync(WindowsHelloEnrollmentStatus.NotEnrolled);
        _helloClient.EnableAsync(Arg.Any<EnableWindowsHelloEnrollmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(WindowsHelloEnrollmentOutcome.Cancelled));

        _value.IsEnabled = true;

        Assert.False(_value.IsEnabled);
        _ = _helloClient.Received(1).EnableAsync(
            Arg.Any<EnableWindowsHelloEnrollmentRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DisableFailureRestoresEnabledState()
    {
        _sessionClient.GetStateAsync(Arg.Any<GetAppSessionStateRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AppSessionStateSnapshot>(new AppSessionStateSnapshot.Locked(Account)));
        _helloClient.GetEnrollmentAsync(Arg.Any<GetWindowsHelloEnrollmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(WindowsHelloEnrollmentStatus.Enrolled));
        var started = WindowsHelloSettingValueTestSupport.NewSource<bool>();
        var disableResponse = WindowsHelloSettingValueTestSupport.NewSource<bool>();
        _helloClient.DisableAsync(Arg.Any<DisableWindowsHelloEnrollmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                started.TrySetResult(true);
                return disableResponse.Task;
            });
        await _value.LoadAsync(TestContext.Current.CancellationToken);

        var restored = WindowsHelloSettingValueTestSupport.NewSource<bool>();
        _value.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_value.IsEnabled) && _value.IsEnabled)
                restored.TrySetResult(true);
        };
        _value.IsEnabled = false;
        await started.Task;
        disableResponse.SetException(new InvalidOperationException("Synthetic disable failure."));
        await restored.Task;

        Assert.True(_value.IsEnabled);
        _ = _helloClient.Received(1).DisableAsync(
            Arg.Any<DisableWindowsHelloEnrollmentRequest>(), Arg.Any<CancellationToken>());
    }

    private async Task LoadActiveAccountAsync(WindowsHelloEnrollmentStatus status)
    {
        _sessionClient.GetStateAsync(Arg.Any<GetAppSessionStateRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AppSessionStateSnapshot>(new AppSessionStateSnapshot.Locked(Account)));
        _helloClient.GetEnrollmentAsync(Arg.Any<GetWindowsHelloEnrollmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(status));
        await _value.LoadAsync(TestContext.Current.CancellationToken);
    }

}
