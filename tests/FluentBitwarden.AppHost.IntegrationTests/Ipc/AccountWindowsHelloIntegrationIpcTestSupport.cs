using BitwardenApi.Notifications.Abstractions;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Ipc;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.IntegrationTests.Ipc;

internal static class AccountWindowsHelloIntegrationIpcTestSupport
{
    internal static UnlockedUserKey ConfigureAccountKeySession(IAppSessionAccessor accessor, UserId userId)
    {
        var userKey = new UnlockedUserKey(userId, [0x10, 0x20, 0x30]);
        IAccountKeySession accountKeySession = Substitute.For<IAccountKeySession>();
        accountKeySession.UserId.Returns(userId);
        accountKeySession.UserKey.Returns(userKey);
        var unlocked = accessor.RequireUnlockedState();
        var state = new AppSessionState.Unlocked(unlocked.Account, unlocked.Vault, accountKeySession, Substitute.For<IServerNotificationsDispatcher>());
        accessor.State.Returns(state);
        return userKey;
    }

    internal static Fixture CreateFixture(bool isSupported, bool isEnabled)
    {
        AccountProfile account = AccountTestData.Profile(
            AccountTestData.FirstUserId,
            "user@example.test",
            "first");
        IAppSessionAccessor accessor = Substitute.For<IAppSessionAccessor, IAsyncDisposable>();
        var vault = Substitute.For<FluentBitwarden.AppHost.Modules.Vault.Contracts.IUnlockedVault>();
        var accountKey = Substitute.For<IAccountKeySession>();
        accountKey.UserId.Returns(account.UserId);
        var state = new AppSessionState.Unlocked(account, vault, accountKey, Substitute.For<IServerNotificationsDispatcher>());
        accessor.State.Returns(state);


        IAccountWindowsHelloService windowsHello = Substitute.For<IAccountWindowsHelloService>();
        windowsHello.IsSupportedAsync().Returns(Task.FromResult(isSupported));
        windowsHello.IsEnabled(account.UserId).Returns(isEnabled);

        return new Fixture(windowsHello, accessor, account);
    }
    internal sealed class Fixture : IAsyncDisposable
    {
        private readonly ServiceProvider _services;
        private readonly AsyncServiceScope _scope;
        public Fixture(IAccountWindowsHelloService windowsHello, IAppSessionAccessor accessor, AccountProfile account)
        {
            WindowsHello = windowsHello;
            Accessor = accessor;
            Account = account;
            var services = new ServiceCollection();
            services.AddSingleton(windowsHello);
            services.AddScoped<IAppSessionAccessor>(_ => accessor);
            services.AddScoped<AccountWindowsHelloIntegrationIpcHandler>();
            _services = services.BuildServiceProvider();
            _scope = _services.CreateAsyncScope();
            Handler = _scope.ServiceProvider.GetRequiredService<AccountWindowsHelloIntegrationIpcHandler>();
        }
        public AccountWindowsHelloIntegrationIpcHandler Handler { get; }
        public IAccountWindowsHelloService WindowsHello { get; }
        public IAppSessionAccessor Accessor { get; }
        public AccountProfile Account { get; }
        public async ValueTask DisposeAsync()
        {
            await _scope.DisposeAsync();
            await _services.DisposeAsync();
        }
    }
}
