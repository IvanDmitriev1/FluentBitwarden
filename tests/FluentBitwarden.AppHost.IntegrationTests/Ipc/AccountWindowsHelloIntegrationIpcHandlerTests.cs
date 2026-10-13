using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Ipc;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using NSubstitute;

namespace FluentBitwarden.AppHost.IntegrationTests.Ipc;

public sealed class AccountWindowsHelloIntegrationIpcHandlerTests
{
    [Fact]
    public async Task Disable_disables_windows_hello_for_the_active_enrolled_account()
    {
        var fixture = CreateFixture(isSupported: true, isEnabled: true);

        await fixture.Handler.DisableAsync(default, TestContext.Current.CancellationToken);

        fixture.WindowsHello.Received(1).Disable(fixture.Account.UserId);
        fixture.Lease.Received(1).Dispose();
    }

    [Fact]
    public async Task Disable_skips_windows_hello_for_the_active_unenrolled_account()
    {
        var fixture = CreateFixture(isSupported: true, isEnabled: false);

        await fixture.Handler.DisableAsync(default, TestContext.Current.CancellationToken);

        fixture.WindowsHello.DidNotReceive().Disable(fixture.Account.UserId);
        fixture.Lease.Received(1).Dispose();
    }

    [Fact]
    public async Task Disable_skips_account_checks_when_windows_hello_is_unsupported()
    {
        var fixture = CreateFixture(isSupported: false, isEnabled: true);

        await fixture.Handler.DisableAsync(default, TestContext.Current.CancellationToken);

        fixture.WindowsHello.DidNotReceive().IsEnabled(fixture.Account.UserId);
        fixture.WindowsHello.DidNotReceive().Disable(fixture.Account.UserId);
        fixture.Lease.Received(1).Dispose();
    }

    private static (AccountWindowsHelloIntegrationIpcHandler Handler, IAccountWindowsHelloService WindowsHello, IUnlockedSessionLease Lease, AccountProfile Account) CreateFixture(
        bool isSupported,
        bool isEnabled)
    {
        AccountProfile account = AccountTestData.Profile(
            AccountTestData.FirstUserId,
            "user@example.test",
            "first");
        IUnlockedSessionLease lease = Substitute.For<IUnlockedSessionLease>();
        lease.Account.Returns(account);

        IAppSessionService sessionService = Substitute.For<IAppSessionService>();
        sessionService.RequireUnlockedSessionLease().Returns(lease);

        IAccountWindowsHelloService windowsHello = Substitute.For<IAccountWindowsHelloService>();
        windowsHello.IsSupportedAsync().Returns(Task.FromResult(isSupported));
        windowsHello.IsEnabled(account.UserId).Returns(isEnabled);

        return (new AccountWindowsHelloIntegrationIpcHandler(windowsHello, sessionService), windowsHello, lease, account);
    }
}
