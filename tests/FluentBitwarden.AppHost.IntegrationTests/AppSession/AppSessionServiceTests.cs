using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.AppSession.Internal;
using FluentBitwarden.AppHost.AppSession.Services;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.AppSession.Unlock;
using FluentBitwarden.Contracts.Infrastructure.WindowsHello;
using FluentBitwarden.Platform.Ipc.Abstractions;
using NSubstitute;

namespace FluentBitwarden.AppHost.IntegrationTests.AppSession;

public sealed class AppSessionServiceTests
{
    [Fact]
    public void Starts_without_an_active_account()
    {
        var context = new SessionTestContext();

        Assert.IsType<AppSessionState.NotAuthenticated>(context.Service.State);
        Assert.Null(GetAccount(context.Service.State));
    }

    [Fact]
    public void RequireUnlockedSessionLease_throws_when_not_authenticated()
    {
        var context = new SessionTestContext();

        Assert.Throws<UnlockedSessionRequiredException>(
            context.Service.RequireUnlockedSessionLease);
    }

    [Fact]
    public async Task RequireUnlockedSessionLease_throws_when_locked()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        var vault = Substitute.For<IUnlockedVault>();
        context.ConfigureAccount(
            account,
            new AccountKeyUnlockResult.Success(new TestAccountKeySession(userKey)),
            vault);
        _ = context.Service.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"));
        await context.Service.LockAsync(TestContext.Current.CancellationToken);

        Assert.Throws<UnlockedSessionRequiredException>(
            context.Service.RequireUnlockedSessionLease);
    }

    [Fact]
    public async Task RequireUnlockedSessionLease_returns_a_lease_that_survives_lock_until_disposed()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        var vault = Substitute.For<IUnlockedVault>();
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(new TestAccountKeySession(userKey)), vault);
        _ = context.Service.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"));

        IUnlockedSessionLease lease = context.Service.RequireUnlockedSessionLease();
        await context.Service.LockAsync(TestContext.Current.CancellationToken);

        Assert.Equal(account, lease.Account);
        Assert.Same(vault, lease.Vault);
        vault.DidNotReceive().Dispose();

        lease.Dispose();

        vault.Received(1).Dispose();
    }

    [Fact]
    public async Task TryGetUnlockedAccount_returns_the_selected_account_only_while_unlocked()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(new TestAccountKeySession(userKey)), Substitute.For<IUnlockedVault>());

        Assert.IsType<SessionUnlockOutcome.Success>(context.Service.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password")));
        Assert.Equal(account, GetAccount(context.Service.State));

        await context.Service.LockAsync(TestContext.Current.CancellationToken);

        Assert.Equal(account, GetAccount(context.Service.State));
    }

    [Fact]
    public void Unlock_with_master_password_opens_the_account_vault()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        var vault = Substitute.For<IUnlockedVault>();
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(new TestAccountKeySession(userKey)), vault);
        const string password = "synthetic-password";

        SessionUnlockOutcome result = context.Service.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, password));

        Assert.IsType<SessionUnlockOutcome.Success>(result);
        Assert.IsType<AppSessionState.Unlocked>(context.Service.State);
        Assert.Equal(account, (context.Service.State switch { AppSessionState.Locked locked => locked.Account, AppSessionState.Unlocked unlocked => unlocked.Account, _ => null }));
        context.AccountService.Received(1).UnlockKey(
            account.UserId,
            new AccountUnlockMethod.MasterPassword(password));
        context.VaultService.Received(1).Open(
            Arg.Is<BitwardenAccountContext>(context => context == account.BitwardenAccountContext),
            Arg.Is<IAccountKeySession>(key => key.UserId == account.UserId));
    }

    [Fact]
    public void Unlock_with_Windows_Hello_forwards_owner_window_and_opens_the_account_vault()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        var vault = Substitute.For<IUnlockedVault>();
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(new TestAccountKeySession(userKey)), vault);
        NativeWindowHandle ownerWindow = new(1234);

        SessionUnlockOutcome result = context.Service.UnlockAsync(
            new SessionUnlockRequest.WindowsHelloRequest(account.UserId, ownerWindow));

        Assert.IsType<SessionUnlockOutcome.Success>(result);
        Assert.IsType<AppSessionState.Unlocked>(context.Service.State);
        context.AccountService.Received(1).UnlockKey(
            account.UserId,
            new AccountUnlockMethod.WindowsHello(ownerWindow));
        context.VaultService.Received(1).Open(
            Arg.Is<BitwardenAccountContext>(context => context == account.BitwardenAccountContext),
            Arg.Is<IAccountKeySession>(key => key.UserId == account.UserId));
    }

    [Fact]
    public void Unlock_when_account_is_missing_returns_failure()
    {
        var context = new SessionTestContext();
        UserId userId = UserId.Parse(AccountTestData.FirstUserId);

        SessionUnlockOutcome result = context.Service.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(userId, "synthetic-password"));

        Assert.Equal(new SessionUnlockOutcome.Failure("Account not found."), result);
        Assert.IsType<AppSessionState.NotAuthenticated>(context.Service.State);
        context.VaultService.DidNotReceive().Open(Arg.Any<BitwardenAccountContext>(), Arg.Any<IAccountKeySession>());
    }

    [Fact]
    public void Unlock_when_key_is_invalid_maps_to_invalid_credentials_failure()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        context.ConfigureAccount(account, new AccountKeyUnlockResult.InvalidCredentials());

        SessionUnlockOutcome result = context.Service.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"));

        Assert.Equal(new SessionUnlockOutcome.Failure("Invalid credentials."), result);
        Assert.IsType<AppSessionState.NotAuthenticated>(context.Service.State);
    }

    [Fact]
    public void Unlock_when_Windows_Hello_is_cancelled_maps_to_cancelled_outcome()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Cancelled());

        SessionUnlockOutcome result = context.Service.UnlockAsync(
            new SessionUnlockRequest.WindowsHelloRequest(account.UserId, new NativeWindowHandle(1234)));

        Assert.IsType<SessionUnlockOutcome.WindowsHelloCancelled>(result);
    }

    [Fact]
    public void Unlock_when_password_unlock_is_cancelled_maps_to_generic_failure()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Cancelled());

        SessionUnlockOutcome result = context.Service.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"));

        Assert.Equal(new SessionUnlockOutcome.Failure("UnlockAsync failed."), result);
    }

    [Fact]
    public void Unlock_when_key_requires_online_reauthentication_maps_to_reauthentication_outcome()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        context.ConfigureAccount(account, new AccountKeyUnlockResult.RequiresOnlineReauthentication());

        SessionUnlockOutcome result = context.Service.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"));

        Assert.IsType<SessionUnlockOutcome.RequiresOnlineReauth>(result);
    }

    [Fact]
    public void Unlock_when_key_failure_is_unclassified_maps_to_generic_failure()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Failure("Synthetic internal failure."));

        SessionUnlockOutcome result = context.Service.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"));

        Assert.Equal(new SessionUnlockOutcome.Failure("UnlockAsync failed."), result);
    }

    [Fact]
    public void Unlock_for_the_same_account_while_unlocked_succeeds_without_reopening_the_vault()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        var vault = Substitute.For<IUnlockedVault>();
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(new TestAccountKeySession(userKey)), vault);
        var request = new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password");

        Assert.IsType<SessionUnlockOutcome.Success>(context.Service.UnlockAsync(
            request));
        Assert.IsType<SessionUnlockOutcome.Success>(context.Service.UnlockAsync(
            request));

        Assert.Equal(account, (context.Service.State switch { AppSessionState.Locked locked => locked.Account, AppSessionState.Unlocked unlocked => unlocked.Account, _ => null }));
        context.VaultService.Received(1).Open(
            Arg.Is<BitwardenAccountContext>(context => context == account.BitwardenAccountContext),
            Arg.Is<IAccountKeySession>(key => key.UserId == account.UserId));
    }

    [Fact]
    public void Unlock_for_a_different_account_while_unlocked_requires_locking_first()
    {
        var context = new SessionTestContext();
        var firstAccount = AccountTestData.Profile(AccountTestData.FirstUserId, "first@example.test", "first");
        var secondAccount = AccountTestData.Profile(AccountTestData.SecondUserId, "second@example.test", "second");
        using var firstUserKey = new UnlockedUserKey(firstAccount.UserId, [0x10, 0x20, 0x30]);
        using var secondUserKey = new UnlockedUserKey(secondAccount.UserId, [0x40, 0x50, 0x60]);
        var firstVault = Substitute.For<IUnlockedVault>();
        var secondVault = Substitute.For<IUnlockedVault>();
        context.ConfigureAccount(firstAccount, new AccountKeyUnlockResult.Success(new TestAccountKeySession(firstUserKey)), firstVault);
        context.ConfigureAccount(secondAccount, new AccountKeyUnlockResult.Success(new TestAccountKeySession(secondUserKey)), secondVault);

        Assert.IsType<SessionUnlockOutcome.Success>(context.Service.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(firstAccount.UserId, "synthetic-password")));

        SessionUnlockOutcome result = context.Service.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(secondAccount.UserId, "synthetic-password"));

        Assert.Equal(
            new SessionUnlockOutcome.Failure("Lock the current account before unlocking another account."),
            result);
        Assert.Equal(firstAccount, (context.Service.State switch { AppSessionState.Locked locked => locked.Account, AppSessionState.Unlocked unlocked => unlocked.Account, _ => null }));
        Assert.IsType<AppSessionState.Unlocked>(context.Service.State);
        context.VaultService.Received(1).Open(
            Arg.Is<BitwardenAccountContext>(context => context == firstAccount.BitwardenAccountContext),
            Arg.Is<IAccountKeySession>(key => key.UserId == firstAccount.UserId));
        context.VaultService.DidNotReceive().Open(
            Arg.Is<BitwardenAccountContext>(context => context == secondAccount.BitwardenAccountContext),
            Arg.Is<IAccountKeySession>(key => key.UserId == secondAccount.UserId));
    }

    [Fact]
    public async Task Lock_then_unlock_again_reopens_the_vault_for_the_account()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        var firstVault = Substitute.For<IUnlockedVault>();
        var secondVault = Substitute.For<IUnlockedVault>();
        firstVault.AccountContext.Returns(account.BitwardenAccountContext);
        secondVault.AccountContext.Returns(account.BitwardenAccountContext);
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(new TestAccountKeySession(userKey)));
        context.VaultService.Open(
            Arg.Is<BitwardenAccountContext>(context => context == account.BitwardenAccountContext),
            Arg.Is<IAccountKeySession>(key => key.UserId == account.UserId)).Returns(firstVault, secondVault);
        var request = new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password");

        Assert.IsType<SessionUnlockOutcome.Success>(context.Service.UnlockAsync(
            request));
        await context.Service.LockAsync(TestContext.Current.CancellationToken);

        Assert.IsType<AppSessionState.Locked>(context.Service.State);
        Assert.IsType<SessionUnlockOutcome.Success>(context.Service.UnlockAsync(
            request));

        Assert.IsType<AppSessionState.Unlocked>(context.Service.State);
        Assert.Equal(account, (context.Service.State switch { AppSessionState.Locked locked => locked.Account, AppSessionState.Unlocked unlocked => unlocked.Account, _ => null }));
        context.VaultService.Received(2).Open(
            Arg.Is<BitwardenAccountContext>(context => context == account.BitwardenAccountContext),
            Arg.Is<IAccountKeySession>(key => key.UserId == account.UserId));
    }

    [Fact]
    public async Task Lock_then_unlock_another_account_keeps_existing_lease_identity_stable()
    {
        var context = new SessionTestContext();
        var firstAccount = AccountTestData.Profile(AccountTestData.FirstUserId, "first@example.test", "first");
        var secondAccount = AccountTestData.Profile(AccountTestData.SecondUserId, "second@example.test", "second");
        using var firstUserKey = new UnlockedUserKey(firstAccount.UserId, [0x10, 0x20, 0x30]);
        using var secondUserKey = new UnlockedUserKey(secondAccount.UserId, [0x40, 0x50, 0x60]);
        var firstVault = Substitute.For<IUnlockedVault>();
        var secondVault = Substitute.For<IUnlockedVault>();
        context.ConfigureAccount(firstAccount, new AccountKeyUnlockResult.Success(new TestAccountKeySession(firstUserKey)), firstVault);
        context.ConfigureAccount(secondAccount, new AccountKeyUnlockResult.Success(new TestAccountKeySession(secondUserKey)), secondVault);

        Assert.IsType<SessionUnlockOutcome.Success>(context.Service.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(firstAccount.UserId, "synthetic-password")));
        IUnlockedSessionLease lease = await context.Service.WaitUntilUnlockedAsync(TestContext.Current.CancellationToken);

        await context.Service.LockAsync(TestContext.Current.CancellationToken);
        Assert.IsType<SessionUnlockOutcome.Success>(context.Service.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(secondAccount.UserId, "synthetic-password")));

        Assert.Equal(secondAccount, (context.Service.State switch { AppSessionState.Locked locked => locked.Account, AppSessionState.Unlocked unlocked => unlocked.Account, _ => null }));
        Assert.Equal(firstAccount, lease.Account);
        Assert.Same(firstVault, lease.Vault);

        await context.Service.LockAsync(TestContext.Current.CancellationToken);
        Assert.IsType<AppSessionState.Locked>(context.Service.State);

        lease.Dispose();
        lease.Dispose();
    }

    [Fact]
    public async Task Failed_switch_after_lock_keeps_the_previous_account_selected_and_locked()
    {
        var context = new SessionTestContext();
        var firstAccount = AccountTestData.Profile(AccountTestData.FirstUserId, "first@example.test", "first");
        var secondAccount = AccountTestData.Profile(AccountTestData.SecondUserId, "second@example.test", "second");
        using var firstUserKey = new UnlockedUserKey(firstAccount.UserId, [0x10, 0x20, 0x30]);
        using var secondUserKey = new UnlockedUserKey(secondAccount.UserId, [0x40, 0x50, 0x60]);
        context.ConfigureAccount(firstAccount, new AccountKeyUnlockResult.Success(new TestAccountKeySession(firstUserKey)), Substitute.For<IUnlockedVault>());
        context.ConfigureAccount(secondAccount, new AccountKeyUnlockResult.InvalidCredentials());

        Assert.IsType<SessionUnlockOutcome.Success>(context.Service.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(firstAccount.UserId, "synthetic-password")));
        await context.Service.LockAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new SessionUnlockOutcome.Failure("Invalid credentials."), context.Service.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(secondAccount.UserId, "synthetic-password")));
        Assert.IsType<AppSessionState.Locked>(context.Service.State);
        Assert.Equal(firstAccount, (context.Service.State switch { AppSessionState.Locked locked => locked.Account, AppSessionState.Unlocked unlocked => unlocked.Account, _ => null }));
    }

    [Fact]
    public void Rejects_a_vault_whose_user_id_does_not_match_the_selected_account()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        var otherAccount = AccountTestData.Profile(AccountTestData.SecondUserId, "second@example.test", "second");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        var mismatchedVault = Substitute.For<IUnlockedVault>();
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(new TestAccountKeySession(userKey)), mismatchedVault);
        mismatchedVault.AccountContext.Returns(otherAccount.BitwardenAccountContext);

        Assert.Throws<InvalidOperationException>(() => context.Service.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password")));

        Assert.IsType<AppSessionState.NotAuthenticated>(context.Service.State);
        Assert.Null((context.Service.State switch { AppSessionState.Locked locked => locked.Account, AppSessionState.Unlocked unlocked => unlocked.Account, _ => null }));
    }

    [Fact]
    public void Rejects_a_vault_from_a_different_environment_for_the_same_user()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        var otherEnvironment = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "other");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        var mismatchedVault = Substitute.For<IUnlockedVault>();
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(new TestAccountKeySession(userKey)), mismatchedVault);
        mismatchedVault.AccountContext.Returns(otherEnvironment.BitwardenAccountContext);

        Assert.Throws<InvalidOperationException>(() => context.Service.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password")));
    }

    [Fact]
    public void Rejects_an_unlocked_vault_with_a_mismatched_account_key()
    {
        var manager = new ActiveSessionManager(Substitute.For<IIpcEventPublisher>());
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        var vault = Substitute.For<IUnlockedVault>();
        vault.AccountContext.Returns(account.BitwardenAccountContext);
        using var accountKey = new TestAccountKeySession(new UnlockedUserKey(UserId.Empty, []));
        using ActiveSessionManager.Transition transition = manager.TryEnterTransition()!;

        Assert.Throws<InvalidOperationException>(() => transition.Unlock(
            new UnlockedVaultLifetime(account, vault, accountKey)));

        Assert.IsType<AppSessionState.NotAuthenticated>(manager.State);
        Assert.Null((manager.State switch { AppSessionState.Locked locked => locked.Account, AppSessionState.Unlocked unlocked => unlocked.Account, _ => null }));
    }

    [Fact]
    public void SignOut_clears_the_selected_account_and_disposes_the_vault()
    {
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        var manager = new ActiveSessionManager(Substitute.For<IIpcEventPublisher>());
        var vault = Substitute.For<IUnlockedVault>();
        vault.AccountContext.Returns(account.BitwardenAccountContext);
        using ActiveSessionManager.Transition transition = manager.TryEnterTransition()!;
        transition.Unlock(new UnlockedVaultLifetime(
            account,
            vault,
            new TestAccountKeySession(new UnlockedUserKey(account.UserId, []))));

        transition.SignOut();

        Assert.IsType<AppSessionState.NotAuthenticated>(manager.State);
        Assert.Null(GetAccount(manager.State));
    }

    [Fact]
    public async Task WaitUntilUnlocked_completes_after_unlock()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        var vault = Substitute.For<IUnlockedVault>();
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(new TestAccountKeySession(userKey)), vault);
        ValueTask<IUnlockedSessionLease> waiter = context.Service.WaitUntilUnlockedAsync(
            TestContext.Current.CancellationToken);

        Assert.IsType<SessionUnlockOutcome.Success>(context.Service.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password")));

        using IUnlockedSessionLease lease = await waiter;
        Assert.Equal(account, lease.Account);
        Assert.Same(vault, lease.Vault);
    }

    [Fact]
    public async Task WaitUntilUnlocked_cancels_when_cancellation_is_requested()
    {
        var context = new SessionTestContext();
        using var cancellation = new CancellationTokenSource();
        ValueTask<IUnlockedSessionLease> waiter = context.Service.WaitUntilUnlockedAsync(cancellation.Token);

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiter.AsTask());
    }

    [Fact]
    public async Task Unlock_waits_for_an_active_transition()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using ActiveSessionManager.Transition transition = await context.ActiveSessionManager.EnterTransition(
            TestContext.Current.CancellationToken);

        Task<SessionUnlockOutcome> unlock = context.Service.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken);

        Assert.False(unlock.IsCompleted);
        transition.Dispose();
        Assert.Equal(new SessionUnlockOutcome.Failure("Account not found."), await unlock);
        Assert.IsType<AppSessionState.NotAuthenticated>(context.Service.State);
    }

    [Fact]
    public async Task Lock_defers_account_key_disposal_until_the_active_session_lease_is_released()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        var vault = Substitute.For<IUnlockedVault>();
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(new TestAccountKeySession(userKey)), vault);
        Assert.IsType<SessionUnlockOutcome.Success>(context.Service.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password")));
        IUnlockedSessionLease lease = await context.Service.WaitUntilUnlockedAsync(
            TestContext.Current.CancellationToken);

        await context.Service.LockAsync(TestContext.Current.CancellationToken);

        Assert.IsType<AppSessionState.Locked>(context.Service.State);
        _ = userKey.Key;
        lease.Dispose();
        lease.Dispose();
        Assert.Throws<ObjectDisposedException>(() => userKey.Key.ToArray());
    }

    private static AccountProfile? GetAccount(AppSessionState state) => state switch
    {
        AppSessionState.Locked locked => locked.Account,
        AppSessionState.Unlocked unlocked => unlocked.Account,
        _ => null
    };

    private sealed class SessionTestContext
    {
        public SessionTestContext()
        {
            AccountService = Substitute.For<IAccountService>();
            VaultService = Substitute.For<IVaultService>();
            ActiveSessionManager = new ActiveSessionManager(Substitute.For<IIpcEventPublisher>());
            Service = new AppSessionService(AccountService, VaultService, ActiveSessionManager);
        }

        public IAccountService AccountService { get; }
        public IVaultService VaultService { get; }
        public ActiveSessionManager ActiveSessionManager { get; }
        public AppSessionService Service { get; }

        public void ConfigureAccount(AccountProfile account, AccountKeyUnlockResult unlockResult, IUnlockedVault? vault = null)
        {
            AccountService.GetAccount(account.UserId).Returns(account);
            AccountService.UnlockKey(account.UserId, Arg.Any<AccountUnlockMethod>()).Returns(unlockResult);

            if (unlockResult is AccountKeyUnlockResult.Success success && vault is not null)
            {
                vault.AccountContext.Returns(account.BitwardenAccountContext);
                VaultService.Open(
                    Arg.Is<BitwardenAccountContext>(context => context == account.BitwardenAccountContext),
                    Arg.Is<IAccountKeySession>(key => key.UserId == account.UserId)).Returns(vault);
            }
        }
    }
}
