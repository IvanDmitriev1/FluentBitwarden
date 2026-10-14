using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Ipc;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using BitwardenApi.Vault.Cryptography;

namespace FluentBitwarden.AppHost.IntegrationTests.Ipc;

internal static class AccountWindowsHelloIntegrationIpcTestSupport
{
    internal static UnlockedUserKey ConfigureAccountKeySession(IUnlockedSessionLease lease, UserId userId)
    {
        var userKey = new UnlockedUserKey(userId, [0x10, 0x20, 0x30]);
        IAccountKeySession accountKeySession = Substitute.For<IAccountKeySession>();
        accountKeySession.UserKey.Returns(userKey);
        lease.AccountKeySession.Returns(accountKeySession);
        return userKey;
    }

    internal static (
        AccountWindowsHelloIntegrationIpcHandler Handler,
        IAccountWindowsHelloService WindowsHello,
        IUnlockedSessionLease Lease,
        AccountProfile Account) CreateFixture(bool isSupported, bool isEnabled)
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
