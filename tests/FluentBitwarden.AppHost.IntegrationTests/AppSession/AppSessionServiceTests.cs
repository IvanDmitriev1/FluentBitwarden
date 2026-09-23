using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.AppSession.Internal;
using FluentBitwarden.AppHost.AppSession.Services;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.AppSession.Status;
using FluentBitwarden.Contracts.AppSession.Unlock;
using FluentBitwarden.Contracts.Infrastructure.WindowsHello;
using NSubstitute;

namespace FluentBitwarden.AppHost.IntegrationTests.AppSession;

public sealed class AppSessionServiceTests
{
    [Fact]
    public void Unlock_with_master_password_opens_the_account_vault()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        var vault = Substitute.For<IUnlockedVault>();
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(userKey), vault);
        const string password = "synthetic-password";

        SessionUnlockOutcome result = context.Service.Unlock(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, password),
            TestContext.Current.CancellationToken);

        Assert.IsType<SessionUnlockOutcome.Success>(result);
        Assert.Equal(AppSessionStatus.Unlocked, context.Service.Snapshot.Status);
        Assert.Equal(account, context.Service.Snapshot.CurrentAccount);
        context.AccountService.Received(1).UnlockKey(
            account.UserId,
            new AccountUnlockMethod.MasterPassword(password));
        context.VaultManager.Received(1).Open(account, userKey);
    }

    [Fact]
    public void Unlock_with_Windows_Hello_forwards_owner_window_and_opens_the_account_vault()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        var vault = Substitute.For<IUnlockedVault>();
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(userKey), vault);
        NativeWindowHandle ownerWindow = new(1234);

        SessionUnlockOutcome result = context.Service.Unlock(
            new SessionUnlockRequest.WindowsHelloRequest(account.UserId, ownerWindow),
            TestContext.Current.CancellationToken);

        Assert.IsType<SessionUnlockOutcome.Success>(result);
        Assert.Equal(AppSessionStatus.Unlocked, context.Service.Snapshot.Status);
        context.AccountService.Received(1).UnlockKey(
            account.UserId,
            new AccountUnlockMethod.WindowsHello(ownerWindow));
        context.VaultManager.Received(1).Open(account, userKey);
    }

    [Fact]
    public void Unlock_when_account_is_missing_returns_failure()
    {
        var context = new SessionTestContext();
        UserId userId = UserId.Parse(AccountTestData.FirstUserId);

        SessionUnlockOutcome result = context.Service.Unlock(
            new SessionUnlockRequest.MasterPasswordRequest(userId, "synthetic-password"),
            TestContext.Current.CancellationToken);

        Assert.Equal(new SessionUnlockOutcome.Failure("Account not found."), result);
        Assert.Equal(AppSessionStatus.NotAuthenticated, context.Service.Snapshot.Status);
        context.VaultManager.DidNotReceive().Open(Arg.Any<AccountProfile>(), Arg.Any<UnlockedUserKey>());
    }

    [Fact]
    public void Unlock_when_key_is_invalid_maps_to_invalid_credentials_failure()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        context.ConfigureAccount(account, new AccountKeyUnlockResult.InvalidCredentials());

        SessionUnlockOutcome result = context.Service.Unlock(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken);

        Assert.Equal(new SessionUnlockOutcome.Failure("Invalid credentials."), result);
        Assert.Equal(AppSessionStatus.NotAuthenticated, context.Service.Snapshot.Status);
    }

    [Fact]
    public void Unlock_when_Windows_Hello_is_cancelled_maps_to_cancelled_outcome()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Cancelled());

        SessionUnlockOutcome result = context.Service.Unlock(
            new SessionUnlockRequest.WindowsHelloRequest(account.UserId, new NativeWindowHandle(1234)),
            TestContext.Current.CancellationToken);

        Assert.IsType<SessionUnlockOutcome.WindowsHelloCancelled>(result);
    }

    [Fact]
    public void Unlock_when_password_unlock_is_cancelled_maps_to_generic_failure()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Cancelled());

        SessionUnlockOutcome result = context.Service.Unlock(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken);

        Assert.Equal(new SessionUnlockOutcome.Failure("Unlock failed."), result);
    }

    [Fact]
    public void Unlock_when_key_requires_online_reauthentication_maps_to_reauthentication_outcome()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        context.ConfigureAccount(account, new AccountKeyUnlockResult.RequiresOnlineReauthentication());

        SessionUnlockOutcome result = context.Service.Unlock(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken);

        Assert.IsType<SessionUnlockOutcome.RequiresOnlineReauth>(result);
    }

    [Fact]
    public void Unlock_when_key_failure_is_unclassified_maps_to_generic_failure()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Failure("Synthetic internal failure."));

        SessionUnlockOutcome result = context.Service.Unlock(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken);

        Assert.Equal(new SessionUnlockOutcome.Failure("Unlock failed."), result);
    }

    [Fact]
    public void Unlock_for_the_same_account_while_unlocked_succeeds_without_reopening_the_vault()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        var vault = Substitute.For<IUnlockedVault>();
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(userKey), vault);
        var request = new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password");

        Assert.IsType<SessionUnlockOutcome.Success>(context.Service.Unlock(
            request,
            TestContext.Current.CancellationToken));
        Assert.IsType<SessionUnlockOutcome.Success>(context.Service.Unlock(
            request,
            TestContext.Current.CancellationToken));

        Assert.Equal(account, context.Service.Snapshot.CurrentAccount);
        context.VaultManager.Received(1).Open(account, userKey);
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
        context.ConfigureAccount(firstAccount, new AccountKeyUnlockResult.Success(firstUserKey), firstVault);
        context.ConfigureAccount(secondAccount, new AccountKeyUnlockResult.Success(secondUserKey), secondVault);

        Assert.IsType<SessionUnlockOutcome.Success>(context.Service.Unlock(
            new SessionUnlockRequest.MasterPasswordRequest(firstAccount.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken));

        SessionUnlockOutcome result = context.Service.Unlock(
            new SessionUnlockRequest.MasterPasswordRequest(secondAccount.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            new SessionUnlockOutcome.Failure("Lock the current account before unlocking another account."),
            result);
        Assert.Equal(firstAccount, context.Service.Snapshot.CurrentAccount);
        Assert.Equal(AppSessionStatus.Unlocked, context.Service.Snapshot.Status);
        context.VaultManager.Received(1).Open(firstAccount, firstUserKey);
        context.VaultManager.DidNotReceive().Open(secondAccount, secondUserKey);
    }

    [Fact]
    public async Task Lock_then_unlock_again_reopens_the_vault_for_the_account()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        var firstVault = Substitute.For<IUnlockedVault>();
        var secondVault = Substitute.For<IUnlockedVault>();
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(userKey));
        context.VaultManager.Open(account, userKey).Returns(firstVault, secondVault);
        var request = new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password");

        Assert.IsType<SessionUnlockOutcome.Success>(context.Service.Unlock(
            request,
            TestContext.Current.CancellationToken));
        await context.Service.LockAsync(TestContext.Current.CancellationToken);

        Assert.Equal(AppSessionStatus.Locked, context.Service.Snapshot.Status);
        firstVault.Received(1).Dispose();
        Assert.IsType<SessionUnlockOutcome.Success>(context.Service.Unlock(
            request,
            TestContext.Current.CancellationToken));

        Assert.Equal(AppSessionStatus.Unlocked, context.Service.Snapshot.Status);
        Assert.Equal(account, context.Service.Snapshot.CurrentAccount);
        context.VaultManager.Received(2).Open(account, userKey);
        secondVault.DidNotReceive().Dispose();
    }

    [Fact]
    public async Task WaitUntilUnlocked_completes_after_unlock()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        var vault = Substitute.For<IUnlockedVault>();
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(userKey), vault);
        ValueTask<IUnlockedSessionLease> waiter = context.Service.WaitUntilUnlockedAsync(
            TestContext.Current.CancellationToken);

        Assert.IsType<SessionUnlockOutcome.Success>(context.Service.Unlock(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken));

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
    public async Task Unlock_during_an_active_transition_returns_concurrent_request()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using ActiveSessionManager.Transition transition = await context.ActiveSessionManager.EnterTransition(
            TestContext.Current.CancellationToken);

        SessionUnlockOutcome result = context.Service.Unlock(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken);

        Assert.IsType<SessionUnlockOutcome.ConcurrentRequest>(result);
        Assert.Equal(AppSessionStatus.NotAuthenticated, context.Service.Snapshot.Status);
    }

    [Fact]
    public async Task Lock_defers_vault_disposal_until_the_active_session_lease_is_released()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        var vault = Substitute.For<IUnlockedVault>();
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(userKey), vault);
        Assert.IsType<SessionUnlockOutcome.Success>(context.Service.Unlock(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken));
        IUnlockedSessionLease lease = await context.Service.WaitUntilUnlockedAsync(
            TestContext.Current.CancellationToken);

        await context.Service.LockAsync(TestContext.Current.CancellationToken);

        Assert.Equal(AppSessionStatus.Locked, context.Service.Snapshot.Status);
        vault.DidNotReceive().Dispose();
        lease.Dispose();
        vault.Received(1).Dispose();
    }

    private sealed class SessionTestContext
    {
        public SessionTestContext()
        {
            AccountService = Substitute.For<IAccountService>();
            VaultManager = Substitute.For<IVaultManager>();
            ActiveSessionManager = new ActiveSessionManager();
            Service = new AppSessionService(AccountService, VaultManager, ActiveSessionManager);
        }

        public IAccountService AccountService { get; }
        public IVaultManager VaultManager { get; }
        public ActiveSessionManager ActiveSessionManager { get; }
        public AppSessionService Service { get; }

        public void ConfigureAccount(AccountProfile account, AccountKeyUnlockResult unlockResult, IUnlockedVault? vault = null)
        {
            AccountService.GetAccount(account.UserId).Returns(account);
            AccountService.UnlockKey(account.UserId, Arg.Any<AccountUnlockMethod>()).Returns(unlockResult);

            if (unlockResult is AccountKeyUnlockResult.Success success && vault is not null)
                VaultManager.Open(account, success.UserKey).Returns(vault);
        }
    }
}
