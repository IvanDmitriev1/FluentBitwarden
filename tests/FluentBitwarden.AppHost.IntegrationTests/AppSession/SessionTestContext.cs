using BitwardenApi.Notifications.Abstractions;
using FluentBitwarden.AppHost.Application.Abstractions;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.AppSession.Services;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.AppSession.State;

namespace FluentBitwarden.AppHost.IntegrationTests.AppSession;

internal sealed class SessionTestContext
{
    public SessionTestContext()
    {
        AccountService = Substitute.For<IAccountService>();
        VaultService = Substitute.For<IVaultService>();
        Store = new AppSessionStore(Substitute.For<FluentBitwarden.Platform.Ipc.Abstractions.IIpcEventPublisher>());
        AppHostTermination = Substitute.For<IAppHostTermination>();
        Operations = new AppSessionOperations(
            AccountService,
            VaultService,
            AppHostTermination,
            Store,
            Substitute.For<IServerNotificationsDispatcherFactory>());
    }

    public IAccountService AccountService { get; }
    public IVaultService VaultService { get; }
    public AppSessionStore Store { get; }
    public IAppHostTermination AppHostTermination { get; }
    public AppSessionOperations Operations { get; }

    public async ValueTask<IAppSessionAccessor> RequireUnlockedSessionLeaseAsync()
    {
        var lease = Store.CreateLease();
        try
        {
            _ = lease.RequireUnlockedState();
            return lease;
        }
        catch
        {
            await lease.DisposeAsync();
            throw;
        }
    }

    public void ConfigureAccount(AccountProfile account, AccountKeyUnlockResult unlockResult, IUnlockedVault? vault = null)
    {
        if (Store.State is AppSessionStateSnapshot.NotAuthenticated)
            Store.AuthenticateAsync(account, CancellationToken.None).GetAwaiter().GetResult();

        AccountService.GetAccount(account.UserId).Returns(account);
        AccountService.UnlockKey(account.UserId, Arg.Any<AccountUnlockMethod>()).Returns(unlockResult);

        if (unlockResult is AccountKeyUnlockResult.Success && vault is not null)
        {
            VaultService.Open(
                Arg.Is<BitwardenAccountContext>(context => context == account.BitwardenAccountContext),
                Arg.Is<IAccountKeySession>(key => key.UserId == account.UserId)).Returns(vault);
        }
    }
}

internal static class AppSessionTestSupport
{
    public static AccountProfile? GetAccount(AppSessionStateSnapshot stateSnapshot) => stateSnapshot switch
    {
        AppSessionStateSnapshot.Locked locked => locked.Account,
        AppSessionStateSnapshot.Unlocked unlocked => unlocked.Account,
        _ => null
    };
}
