using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.AppSession.Internal;
using FluentBitwarden.AppHost.AppSession.Services;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.IntegrationTests.AppSession;

internal sealed class SessionTestContext
{
    public SessionTestContext()
    {
        AccountService = Substitute.For<IAccountService>();
        VaultService = Substitute.For<IVaultService>();
        ActiveSessionManager = new ActiveSessionManager(Substitute.For<IIpcEventPublisher>());
        AppHostTermination = Substitute.For<IAppHostTermination>();
        Service = new AppSessionService(AccountService, VaultService, ActiveSessionManager, AppHostTermination);
    }

    public IAccountService AccountService { get; }
    public IVaultService VaultService { get; }
    public ActiveSessionManager ActiveSessionManager { get; }
    public IAppHostTermination AppHostTermination { get; }
    public AppSessionService Service { get; }

    public void ConfigureAccount(AccountProfile account, AccountKeyUnlockResult unlockResult, IUnlockedVault? vault = null)
    {
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
    public static AccountProfile? GetAccount(AppSessionState state) => state switch
    {
        AppSessionState.Locked locked => locked.Account,
        AppSessionState.Unlocked unlocked => unlocked.Account,
        _ => null
    };
}
