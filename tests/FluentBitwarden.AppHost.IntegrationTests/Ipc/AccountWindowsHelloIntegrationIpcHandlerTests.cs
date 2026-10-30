using System.Security.Cryptography;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.Contracts.Infrastructure.WindowsHello;
using static FluentBitwarden.AppHost.IntegrationTests.Ipc.AccountWindowsHelloIntegrationIpcTestSupport;

namespace FluentBitwarden.AppHost.IntegrationTests.Ipc;

public sealed class AccountWindowsHelloIntegrationIpcHandlerTests
{
    [Fact]
    public async Task GetEnrollment_returns_enrolled_for_the_requested_account()
    {
        await using var fixture = CreateFixture(isSupported: true, isEnabled: false);
        UserId requestedUserId = UserId.Parse(AccountTestData.SecondUserId);
        fixture.WindowsHello.IsEnabled(requestedUserId).Returns(true);

        WindowsHelloEnrollmentStatus status = await fixture.Handler.GetEnrollmentAsync(
            new GetWindowsHelloEnrollmentRequest(requestedUserId),
            TestContext.Current.CancellationToken);

        Assert.Equal(WindowsHelloEnrollmentStatus.Enrolled, status);
        fixture.WindowsHello.Received(1).IsEnabled(requestedUserId);
        fixture.WindowsHello.DidNotReceive().IsEnabled(fixture.Account.UserId);
    }

    [Fact]
    public async Task GetEnrollment_returns_not_enrolled_for_the_requested_account()
    {
        await using var fixture = CreateFixture(isSupported: true, isEnabled: true);
        UserId requestedUserId = UserId.Parse(AccountTestData.SecondUserId);
        fixture.WindowsHello.IsEnabled(requestedUserId).Returns(false);

        WindowsHelloEnrollmentStatus status = await fixture.Handler.GetEnrollmentAsync(
            new GetWindowsHelloEnrollmentRequest(requestedUserId),
            TestContext.Current.CancellationToken);

        Assert.Equal(WindowsHelloEnrollmentStatus.NotEnrolled, status);
        fixture.WindowsHello.Received(1).IsEnabled(requestedUserId);
        fixture.WindowsHello.DidNotReceive().IsEnabled(fixture.Account.UserId);
    }

    [Fact]
    public async Task GetEnrollment_returns_unavailable_without_account_lookup_when_unsupported()
    {
        await using var fixture = CreateFixture(isSupported: false, isEnabled: true);
        UserId requestedUserId = UserId.Parse(AccountTestData.SecondUserId);

        WindowsHelloEnrollmentStatus status = await fixture.Handler.GetEnrollmentAsync(
            new GetWindowsHelloEnrollmentRequest(requestedUserId),
            TestContext.Current.CancellationToken);

        Assert.Equal(WindowsHelloEnrollmentStatus.Unavailable, status);
        fixture.WindowsHello.DidNotReceive().IsEnabled(Arg.Any<UserId>());
    }

    [Fact]
    public async Task Disable_disables_windows_hello_for_the_active_enrolled_account()
    {
        await using var fixture = CreateFixture(isSupported: true, isEnabled: true);

        await fixture.Handler.DisableAsync(default, TestContext.Current.CancellationToken);

        fixture.WindowsHello.Received(1).Disable(fixture.Account.UserId);
        await fixture.DisposeAsync();
        await fixture.Lease.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task Disable_skips_windows_hello_for_the_active_unenrolled_account()
    {
        await using var fixture = CreateFixture(isSupported: true, isEnabled: false);

        await fixture.Handler.DisableAsync(default, TestContext.Current.CancellationToken);

        fixture.WindowsHello.DidNotReceive().Disable(fixture.Account.UserId);
        await fixture.DisposeAsync();
        await fixture.Lease.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task Disable_skips_account_checks_when_windows_hello_is_unsupported()
    {
        await using var fixture = CreateFixture(isSupported: false, isEnabled: true);

        await fixture.Handler.DisableAsync(default, TestContext.Current.CancellationToken);

        fixture.WindowsHello.DidNotReceive().IsEnabled(fixture.Account.UserId);
        fixture.WindowsHello.DidNotReceive().Disable(fixture.Account.UserId);
        await fixture.DisposeAsync();
        await fixture.Lease.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task Enable_forwards_the_active_user_key_and_owner_window()
    {
        await using var fixture = CreateFixture(isSupported: true, isEnabled: false);
        using UnlockedUserKey userKey = ConfigureAccountKeySession(fixture.Lease, fixture.Account.UserId);
        var request = new EnableWindowsHelloEnrollmentRequest(new NativeWindowHandle(0x1234));

        WindowsHelloEnrollmentOutcome outcome = await fixture.Handler.EnableAsync(
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(WindowsHelloEnrollmentOutcome.Enrolled, outcome);
        fixture.WindowsHello.Received(1).Enable(userKey, (IntPtr)request.OwnerWindow);
        await fixture.DisposeAsync();
        await fixture.Lease.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task Enable_returns_enrolled_without_prompting_when_the_account_is_already_enrolled()
    {
        await using var fixture = CreateFixture(isSupported: true, isEnabled: true);

        WindowsHelloEnrollmentOutcome outcome = await fixture.Handler.EnableAsync(
            default,
            TestContext.Current.CancellationToken);

        Assert.Equal(WindowsHelloEnrollmentOutcome.Enrolled, outcome);
        fixture.WindowsHello.DidNotReceive().Enable(Arg.Any<UnlockedUserKey>(), Arg.Any<IntPtr>());
        await fixture.DisposeAsync();
        await fixture.Lease.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task Enable_returns_unavailable_without_checking_enrollment_when_unsupported()
    {
        await using var fixture = CreateFixture(isSupported: false, isEnabled: true);

        WindowsHelloEnrollmentOutcome outcome = await fixture.Handler.EnableAsync(
            default,
            TestContext.Current.CancellationToken);

        Assert.Equal(WindowsHelloEnrollmentOutcome.Unavailable, outcome);
        fixture.WindowsHello.DidNotReceive().IsEnabled(fixture.Account.UserId);
        fixture.WindowsHello.DidNotReceive().Enable(Arg.Any<UnlockedUserKey>(), Arg.Any<IntPtr>());
        await fixture.DisposeAsync();
        await fixture.Lease.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task Enable_maps_user_cancellation_and_releases_the_session_lease()
    {
        await using var fixture = CreateFixture(isSupported: true, isEnabled: false);
        using UnlockedUserKey userKey = ConfigureAccountKeySession(fixture.Lease, fixture.Account.UserId);
        fixture.WindowsHello.When(service => service.Enable(userKey, Arg.Any<IntPtr>()))
            .Do(_ => throw new OperationCanceledException());

        WindowsHelloEnrollmentOutcome outcome = await fixture.Handler.EnableAsync(
            default,
            TestContext.Current.CancellationToken);

        Assert.Equal(WindowsHelloEnrollmentOutcome.Cancelled, outcome);
        await fixture.DisposeAsync();
        await fixture.Lease.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task Enable_maps_cryptographic_failures_and_releases_the_session_lease()
    {
        await using var fixture = CreateFixture(isSupported: true, isEnabled: false);
        using UnlockedUserKey userKey = ConfigureAccountKeySession(fixture.Lease, fixture.Account.UserId);
        fixture.WindowsHello.When(service => service.Enable(userKey, Arg.Any<IntPtr>()))
            .Do(_ => throw new CryptographicException("synthetic enrollment failure"));

        WindowsHelloEnrollmentOutcome outcome = await fixture.Handler.EnableAsync(
            default,
            TestContext.Current.CancellationToken);

        Assert.Equal(WindowsHelloEnrollmentOutcome.Failed, outcome);
        await fixture.DisposeAsync();
        await fixture.Lease.Received(1).DisposeAsync();
    }
}
