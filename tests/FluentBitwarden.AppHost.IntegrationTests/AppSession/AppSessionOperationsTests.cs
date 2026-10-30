using BitwardenApi.Notifications.Abstractions;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.AppSession.Services;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.AppSession.Unlock;
using FluentBitwarden.Contracts.Infrastructure.WindowsHello;

namespace FluentBitwarden.AppHost.IntegrationTests.AppSession;

public sealed class AppSessionOperationsTests
{
    [Fact]
    public void Starts_without_an_active_account()
    {
        var context = new SessionTestContext();

        Assert.IsType<AppSessionStateSnapshot.NotAuthenticated>(context.Store.State);
        Assert.Null(AppSessionTestSupport.GetAccount(context.Store.State));
    }

    [Fact]
    public async Task RequireUnlockedSessionLease_throws_when_not_authenticated()
    {
        var context = new SessionTestContext();

        await Assert.ThrowsAsync<UnlockedSessionRequiredException>(
            async () => await context.RequireUnlockedSessionLeaseAsync());
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
        _ = await context.Operations.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken);
        await context.Operations.LockAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<UnlockedSessionRequiredException>(
            async () => await context.RequireUnlockedSessionLeaseAsync());
    }

    [Fact]
    public async Task RequireUnlockedSessionLease_returns_a_lease_that_survives_lock_until_disposed()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        var vault = Substitute.For<IUnlockedVault>();
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(new TestAccountKeySession(userKey)), vault);
        _ = await context.Operations.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken);

        IAppSessionAccessor lease = await context.RequireUnlockedSessionLeaseAsync();
        await context.Operations.LockAsync(TestContext.Current.CancellationToken);

        Assert.Equal(account, lease.RequireUnlockedState().Account);
        Assert.Same(vault, lease.RequireUnlockedState().Vault);
        vault.DidNotReceive().Dispose();

        await lease.DisposeAsync();

        vault.Received(1).Dispose();
    }

    [Fact]
    public async Task TryGetUnlockedAccount_returns_the_selected_account_only_while_unlocked()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(new TestAccountKeySession(userKey)), Substitute.For<IUnlockedVault>());

        Assert.IsType<SessionUnlockOutcome.Success>(await context.Operations.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken));
        Assert.Equal(account, AppSessionTestSupport.GetAccount(context.Store.State));

        await context.Operations.LockAsync(TestContext.Current.CancellationToken);

        Assert.Equal(account, AppSessionTestSupport.GetAccount(context.Store.State));
    }

    [Fact]
    public async Task Unlock_with_master_password_opens_the_account_vault()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        var vault = Substitute.For<IUnlockedVault>();
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(new TestAccountKeySession(userKey)), vault);
        const string password = "synthetic-password";

        SessionUnlockOutcome result = await context.Operations.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, password),
            TestContext.Current.CancellationToken);

        Assert.IsType<SessionUnlockOutcome.Success>(result);
        Assert.IsType<AppSessionStateSnapshot.Unlocked>(context.Store.State);
        context.AppHostTermination.DidNotReceive().TerminateApplication();
        Assert.Equal(account, (context.Store.State switch { AppSessionStateSnapshot.Locked locked => locked.Account, AppSessionStateSnapshot.Unlocked unlocked => unlocked.Account, _ => null }));
        context.AccountService.Received(1).UnlockKey(
            account.UserId,
            new AccountUnlockMethod.MasterPassword(password));
        context.VaultService.Received(1).Open(
            Arg.Is<BitwardenAccountContext>(context => context == account.BitwardenAccountContext),
            Arg.Is<IAccountKeySession>(key => key.UserId == account.UserId));
    }

    [Fact]
    public async Task Unlock_when_opening_the_vault_throws_requests_application_termination()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        var expectedException = new InvalidOperationException("Vault open failed.");
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(new TestAccountKeySession(userKey)));
        context.AppHostTermination.When(termination => termination.TerminateApplication())
            .Do(_ => throw new TerminationRequestedException());
        context.VaultService.Open(account.BitwardenAccountContext, Arg.Any<IAccountKeySession>())
            .Returns<IUnlockedVault>(_ => throw expectedException);

        await Assert.ThrowsAsync<TerminationRequestedException>(() =>
            context.Operations.UnlockAsync(
                new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"),
                TestContext.Current.CancellationToken));

        context.AppHostTermination.Received(1).TerminateApplication();
        Assert.IsType<AppSessionStateSnapshot.Locked>(context.Store.State);
    }

    [Fact]
    public async Task Unlock_when_lifetime_creation_fails_requests_application_termination()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var userKey = new UnlockedUserKey(UserId.Empty, []);
        var vault = Substitute.For<IUnlockedVault>();
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(new TestAccountKeySession(userKey)));
        context.AppHostTermination.When(termination => termination.TerminateApplication())
            .Do(_ => throw new TerminationRequestedException());
        context.VaultService.Open(account.BitwardenAccountContext, Arg.Any<IAccountKeySession>()).Returns(vault);

        await Assert.ThrowsAsync<TerminationRequestedException>(() => context.Operations.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken));

        context.AppHostTermination.Received(1).TerminateApplication();
        Assert.IsType<AppSessionStateSnapshot.Locked>(context.Store.State);
    }

    [Fact]
    public async Task Unlock_with_Windows_Hello_forwards_owner_window_and_opens_the_account_vault()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        var vault = Substitute.For<IUnlockedVault>();
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(new TestAccountKeySession(userKey)), vault);
        NativeWindowHandle ownerWindow = new(1234);

        SessionUnlockOutcome result = await context.Operations.UnlockAsync(
            new SessionUnlockRequest.WindowsHelloRequest(account.UserId, ownerWindow),
            TestContext.Current.CancellationToken);

        Assert.IsType<SessionUnlockOutcome.Success>(result);
        Assert.IsType<AppSessionStateSnapshot.Unlocked>(context.Store.State);
        context.AppHostTermination.DidNotReceive().TerminateApplication();
        context.AccountService.Received(1).UnlockKey(
            account.UserId,
            new AccountUnlockMethod.WindowsHello(ownerWindow));
        context.VaultService.Received(1).Open(
            Arg.Is<BitwardenAccountContext>(context => context == account.BitwardenAccountContext),
            Arg.Is<IAccountKeySession>(key => key.UserId == account.UserId));
    }

    [Fact]
    public async Task Unlock_when_account_is_missing_returns_failure()
    {
        var context = new SessionTestContext();
        UserId userId = UserId.Parse(AccountTestData.FirstUserId);

        SessionUnlockOutcome result = await context.Operations.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(userId, "synthetic-password"),
            TestContext.Current.CancellationToken);

        Assert.Equal(new SessionUnlockOutcome.Failure("Account not found."), result);
        Assert.IsType<AppSessionStateSnapshot.NotAuthenticated>(context.Store.State);
        context.VaultService.DidNotReceive().Open(Arg.Any<BitwardenAccountContext>(), Arg.Any<IAccountKeySession>());
    }

    [Fact]
    public async Task Unlock_when_key_is_invalid_maps_to_invalid_credentials_failure()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        context.ConfigureAccount(account, new AccountKeyUnlockResult.InvalidCredentials());

        SessionUnlockOutcome result = await context.Operations.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken);

        Assert.Equal(new SessionUnlockOutcome.Failure("Invalid credentials."), result);
        Assert.IsType<AppSessionStateSnapshot.Locked>(context.Store.State);
        context.AppHostTermination.DidNotReceive().TerminateApplication();
    }

    [Fact]
    public async Task Unlock_when_Windows_Hello_is_cancelled_maps_to_cancelled_outcome()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Cancelled());

        SessionUnlockOutcome result = await context.Operations.UnlockAsync(
            new SessionUnlockRequest.WindowsHelloRequest(account.UserId, new NativeWindowHandle(1234)),
            TestContext.Current.CancellationToken);

        Assert.IsType<SessionUnlockOutcome.WindowsHelloCancelled>(result);
    }

    [Fact]
    public async Task Unlock_when_password_unlock_is_cancelled_maps_to_generic_failure()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Cancelled());

        SessionUnlockOutcome result = await context.Operations.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken);

        Assert.Equal(new SessionUnlockOutcome.Failure("UnlockAsync failed."), result);
    }

    [Fact]
    public async Task Unlock_when_key_requires_online_reauthentication_maps_to_reauthentication_outcome()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        context.ConfigureAccount(account, new AccountKeyUnlockResult.RequiresOnlineReauthentication());

        SessionUnlockOutcome result = await context.Operations.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken);

        Assert.IsType<SessionUnlockOutcome.RequiresOnlineReauth>(result);
    }

    [Fact]
    public async Task Unlock_when_key_failure_is_unclassified_maps_to_generic_failure()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Failure("Synthetic internal failure."));

        SessionUnlockOutcome result = await context.Operations.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken);

        Assert.Equal(new SessionUnlockOutcome.Failure("UnlockAsync failed."), result);
    }

    [Fact]
    public async Task Unlock_for_the_same_account_while_unlocked_succeeds_without_reopening_the_vault()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        var vault = Substitute.For<IUnlockedVault>();
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(new TestAccountKeySession(userKey)), vault);
        var request = new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password");

        Assert.IsType<SessionUnlockOutcome.Success>(await context.Operations.UnlockAsync(
            request,
            TestContext.Current.CancellationToken));
        Assert.IsType<SessionUnlockOutcome.Success>(await context.Operations.UnlockAsync(
            request,
            TestContext.Current.CancellationToken));

        Assert.Equal(account, (context.Store.State switch { AppSessionStateSnapshot.Locked locked => locked.Account, AppSessionStateSnapshot.Unlocked unlocked => unlocked.Account, _ => null }));
        context.VaultService.Received(1).Open(
            Arg.Is<BitwardenAccountContext>(context => context == account.BitwardenAccountContext),
            Arg.Is<IAccountKeySession>(key => key.UserId == account.UserId));
    }

    [Fact]
    public async Task Unlock_for_a_different_account_while_unlocked_requires_locking_first()
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

        Assert.IsType<SessionUnlockOutcome.Success>(await context.Operations.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(firstAccount.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken));

        SessionUnlockOutcome result = await context.Operations.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(secondAccount.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            new SessionUnlockOutcome.Failure("Lock the current account before unlocking another account."),
            result);
        Assert.Equal(firstAccount, (context.Store.State switch { AppSessionStateSnapshot.Locked locked => locked.Account, AppSessionStateSnapshot.Unlocked unlocked => unlocked.Account, _ => null }));
        Assert.IsType<AppSessionStateSnapshot.Unlocked>(context.Store.State);
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
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(new TestAccountKeySession(userKey)));
        context.VaultService.Open(
            Arg.Is<BitwardenAccountContext>(context => context == account.BitwardenAccountContext),
            Arg.Is<IAccountKeySession>(key => key.UserId == account.UserId)).Returns(firstVault, secondVault);
        var request = new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password");

        Assert.IsType<SessionUnlockOutcome.Success>(await context.Operations.UnlockAsync(
            request,
            TestContext.Current.CancellationToken));
        await context.Operations.LockAsync(TestContext.Current.CancellationToken);

        Assert.IsType<AppSessionStateSnapshot.Locked>(context.Store.State);
        Assert.IsType<SessionUnlockOutcome.Success>(await context.Operations.UnlockAsync(
            request,
            TestContext.Current.CancellationToken));

        Assert.IsType<AppSessionStateSnapshot.Unlocked>(context.Store.State);
        Assert.Equal(account, (context.Store.State switch { AppSessionStateSnapshot.Locked locked => locked.Account, AppSessionStateSnapshot.Unlocked unlocked => unlocked.Account, _ => null }));
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

        Assert.IsType<SessionUnlockOutcome.Success>(await context.Operations.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(firstAccount.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken));
        IAppSessionAccessor lease = await context.Store.WaitUntilUnlockedAsync(TestContext.Current.CancellationToken);

        await context.Operations.LockAsync(TestContext.Current.CancellationToken);
        Assert.IsType<SessionUnlockOutcome.Success>(await context.Operations.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(secondAccount.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken));

        Assert.Equal(secondAccount, (context.Store.State switch { AppSessionStateSnapshot.Locked locked => locked.Account, AppSessionStateSnapshot.Unlocked unlocked => unlocked.Account, _ => null }));
        Assert.Equal(firstAccount, lease.RequireUnlockedState().Account);
        Assert.Same(firstVault, lease.RequireUnlockedState().Vault);

        await context.Operations.LockAsync(TestContext.Current.CancellationToken);
        Assert.IsType<AppSessionStateSnapshot.Locked>(context.Store.State);

        await lease.DisposeAsync();
        await lease.DisposeAsync();
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

        Assert.IsType<SessionUnlockOutcome.Success>(await context.Operations.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(firstAccount.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken));
        await context.Operations.LockAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new SessionUnlockOutcome.Failure("Invalid credentials."), await context.Operations.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(secondAccount.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken));
        Assert.IsType<AppSessionStateSnapshot.Locked>(context.Store.State);
        Assert.Equal(firstAccount, (context.Store.State switch { AppSessionStateSnapshot.Locked locked => locked.Account, AppSessionStateSnapshot.Unlocked unlocked => unlocked.Account, _ => null }));
    }

    [Fact]
    public async Task Rejects_an_unlocked_vault_with_a_mismatched_account_key()
    {
        var store = new AppSessionStore(Substitute.For<FluentBitwarden.Platform.Ipc.Abstractions.IIpcEventPublisher>());
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        var vault = Substitute.For<IUnlockedVault>();
        using var accountKey = new TestAccountKeySession(new UnlockedUserKey(UserId.Empty, []));
        await store.AuthenticateAsync(account, TestContext.Current.CancellationToken);
        Assert.Throws<InvalidOperationException>(() =>
            new AppSessionState.Unlocked(account, vault, accountKey, Substitute.For<IServerNotificationsClient>()));

        Assert.IsType<AppSessionStateSnapshot.Locked>(store.State);
    }

    [Fact]
    public async Task SignOut_clears_the_selected_account_and_disposes_the_vault()
    {
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        var store = new AppSessionStore(Substitute.For<FluentBitwarden.Platform.Ipc.Abstractions.IIpcEventPublisher>());
        var vault = Substitute.For<IUnlockedVault>();
        await store.AuthenticateAsync(account, TestContext.Current.CancellationToken);
        await store.UnlockAsync(new AppSessionState.Unlocked(account, vault,
            new TestAccountKeySession(new UnlockedUserKey(account.UserId, [])), Substitute.For<IServerNotificationsClient>()));

        await store.SignOutAsync(TestContext.Current.CancellationToken);

        Assert.IsType<AppSessionStateSnapshot.NotAuthenticated>(store.State);
        Assert.Null(AppSessionTestSupport.GetAccount(store.State));
        vault.Received(1).Dispose();
    }
    [Fact]
    public async Task WaitUntilUnlocked_completes_after_unlock()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        var vault = Substitute.For<IUnlockedVault>();
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(new TestAccountKeySession(userKey)), vault);
        ValueTask<IAppSessionAccessor> waiter = context.Store.WaitUntilUnlockedAsync(
            TestContext.Current.CancellationToken);

        Assert.IsType<SessionUnlockOutcome.Success>(await context.Operations.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken));

        await using IAppSessionAccessor lease = await waiter;
        Assert.Equal(account, lease.RequireUnlockedState().Account);
        Assert.Same(vault, lease.RequireUnlockedState().Vault);
    }

    [Fact]
    public async Task WaitUntilUnlocked_cancels_when_cancellation_is_requested()
    {
        var context = new SessionTestContext();
        using var cancellation = new CancellationTokenSource();
        ValueTask<IAppSessionAccessor> waiter = context.Store.WaitUntilUnlockedAsync(cancellation.Token);

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiter.AsTask());
    }

    [Fact]
    public async Task Unlock_honors_cancellation_before_publishing_the_vault()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var key = new TestAccountKeySession(new UnlockedUserKey(account.UserId, []));
        var vault = Substitute.For<IUnlockedVault>();
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(key), vault);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Operations.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"), cancellation.Token));
        Assert.IsType<AppSessionStateSnapshot.Locked>(context.Store.State);
    }
    [Fact]
    public async Task Lock_defers_account_key_disposal_until_the_active_session_lease_is_released()
    {
        var context = new SessionTestContext();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        var vault = Substitute.For<IUnlockedVault>();
        context.ConfigureAccount(account, new AccountKeyUnlockResult.Success(new TestAccountKeySession(userKey)), vault);
        Assert.IsType<SessionUnlockOutcome.Success>(await context.Operations.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken));
        IAppSessionAccessor lease = await context.Store.WaitUntilUnlockedAsync(
            TestContext.Current.CancellationToken);

        await context.Operations.LockAsync(TestContext.Current.CancellationToken);

        Assert.IsType<AppSessionStateSnapshot.Locked>(context.Store.State);
        _ = userKey.Key;
        await lease.DisposeAsync();
        await lease.DisposeAsync();
        Assert.Throws<ObjectDisposedException>(() => userKey.Key.ToArray());
    }

    private sealed class TerminationRequestedException : Exception { }
}
