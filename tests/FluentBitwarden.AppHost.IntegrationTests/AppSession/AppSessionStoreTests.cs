using BitwardenApi.Notifications.Abstractions;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.AppSession.Services;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.IntegrationTests.AppSession;

public sealed class AppSessionStoreTests
{
    private static AccountProfile Account => AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");

    [Fact]
    public async Task Unlock_waits_for_the_previous_transition_before_publishing()
    {
        var publisher = Substitute.For<IIpcEventPublisher>();
        var publication = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        publisher.PublishAsync(Arg.Any<AppSessionStateChangedEvent>(), Arg.Any<CancellationToken>())
            .Returns(publication.Task);
        var store = new AppSessionStore(publisher);
        Task authentication = store.AuthenticateAsync(Account, CancellationToken.None);
        var vault = Substitute.For<IUnlockedVault>();
        var key = Substitute.For<IAccountKeySession>();
        key.UserId.Returns(Account.UserId);
        var candidate = new AppSessionState.Unlocked(Account, vault, key, Substitute.For<IServerNotificationsClient>());
        Task unlock = store.UnlockAsync(candidate);
        try
        {
            Assert.False(unlock.IsCompleted);
            Assert.Equal(Account, Assert.IsType<AppSessionStateSnapshot.Locked>(store.State).Account);
            vault.DidNotReceive().Dispose();
            key.DidNotReceive().Dispose();
        }
        finally
        {
            publication.TrySetResult();
            await authentication;
        }
        await unlock;
        Assert.Equal(Account, Assert.IsType<AppSessionStateSnapshot.Unlocked>(store.State).Account);
        await store.SignOutAsync(CancellationToken.None);
        vault.Received(1).Dispose();
        key.Received(1).Dispose();
    }

    [Fact]
    public async Task Notification_failure_keeps_published_vault_alive_and_retires_previous_state()
    {
        var publisher = Substitute.For<IIpcEventPublisher>();
        var store = new AppSessionStore(publisher);
        await store.AuthenticateAsync(Account, CancellationToken.None);
        await using var previous = store.CreateLease();
        var revoked = previous.Revoked;
        var vault = Substitute.For<IUnlockedVault>();
        var key = Substitute.For<IAccountKeySession>();
        key.UserId.Returns(Account.UserId);
        var candidate = new AppSessionState.Unlocked(Account, vault, key, Substitute.For<IServerNotificationsClient>());
        var failure = new InvalidOperationException("Notification failed.");
        publisher.PublishAsync(Arg.Any<AppSessionStateChangedEvent>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw failure);

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.UnlockAsync(candidate)));
        Assert.IsType<AppSessionStateSnapshot.Unlocked>(store.State);
        Assert.True(revoked.IsCancellationRequested);
        vault.DidNotReceive().Dispose();
        key.DidNotReceive().Dispose();
        publisher.PublishAsync(Arg.Any<AppSessionStateChangedEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        await store.LockAsync(CancellationToken.None);
        vault.Received(1).Dispose();
        key.Received(1).Dispose();
    }

    [Fact]
    public async Task State_snapshots_expose_only_the_state_and_account()
    {
        var store = new AppSessionStore(Substitute.For<FluentBitwarden.Platform.Ipc.Abstractions.IIpcEventPublisher>());
        Assert.IsType<AppSessionStateSnapshot.NotAuthenticated>(store.State);

        await store.AuthenticateAsync(Account, CancellationToken.None);
        Assert.Equal(Account, Assert.IsType<AppSessionStateSnapshot.Locked>(store.State).Account);

        var vault = Substitute.For<IUnlockedVault>();
        var key = Substitute.For<IAccountKeySession>();
        key.UserId.Returns(Account.UserId);
        await store.UnlockAsync(new AppSessionState.Unlocked(Account, vault, key, Substitute.For<IServerNotificationsClient>()));
        var unlocked = Assert.IsType<AppSessionStateSnapshot.Unlocked>(store.State);
        Assert.Equal(Account, unlocked.Account);
        Assert.DoesNotContain("Vault =", unlocked.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("AccountKeySession", unlocked.ToString(), StringComparison.Ordinal);
        await store.SignOutAsync(CancellationToken.None);
        vault.Received(1).Dispose();
        key.Received(1).Dispose();
    }

    [Fact]
    public async Task Duplicate_authentication_is_rejected_and_keeps_the_original_account()
    {
        var store = new AppSessionStore(Substitute.For<FluentBitwarden.Platform.Ipc.Abstractions.IIpcEventPublisher>());
        await store.AuthenticateAsync(Account, CancellationToken.None);
        var other = AccountTestData.Profile(AccountTestData.SecondUserId, "other@example.test", "second");

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.AuthenticateAsync(other, CancellationToken.None));

        Assert.Equal(Account, Assert.IsType<AppSessionStateSnapshot.Locked>(store.State).Account);
    }

    [Fact]
    public async Task Lock_and_signout_are_noops_when_already_locked_or_signed_out()
    {
        var store = new AppSessionStore(Substitute.For<FluentBitwarden.Platform.Ipc.Abstractions.IIpcEventPublisher>());
        await store.LockAsync(CancellationToken.None);
        await store.SignOutAsync(CancellationToken.None);
        await store.AuthenticateAsync(Account, CancellationToken.None);
        await store.LockAsync(CancellationToken.None);
        Assert.Equal(Account, Assert.IsType<AppSessionStateSnapshot.Locked>(store.State).Account);
        await store.SignOutAsync(CancellationToken.None);
        Assert.IsType<AppSessionStateSnapshot.NotAuthenticated>(store.State);
    }

    [Fact]
    public async Task Unlock_requires_locked_state_and_lock_retires_unlocked_state()
    {
        var store = new AppSessionStore(Substitute.For<FluentBitwarden.Platform.Ipc.Abstractions.IIpcEventPublisher>());
        var vault = Substitute.For<IUnlockedVault>();
        var key = Substitute.For<IAccountKeySession>();
        key.UserId.Returns(Account.UserId);
        var state = new AppSessionState.Unlocked(Account, vault, key, Substitute.For<IServerNotificationsClient>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.UnlockAsync(state));
        vault.Received(1).Dispose();
        key.Received(1).Dispose();

        await store.AuthenticateAsync(Account, CancellationToken.None);
        vault = Substitute.For<IUnlockedVault>();
        key = Substitute.For<IAccountKeySession>();
        key.UserId.Returns(Account.UserId);
        await store.UnlockAsync(new AppSessionState.Unlocked(Account, vault, key, Substitute.For<IServerNotificationsClient>()));
        await using var oldLease = store.CreateLease();
        CancellationToken revoked = oldLease.Revoked;

        await store.LockAsync(CancellationToken.None);

        Assert.True(revoked.IsCancellationRequested);
        Assert.IsType<AppSessionStateSnapshot.Locked>(store.State);
        vault.DidNotReceive().Dispose();
        key.DidNotReceive().Dispose();
    }

    [Fact]
    public void Unlocked_state_requires_matching_account_key()
    {
        var otherKey = Substitute.For<IAccountKeySession>();
        otherKey.UserId.Returns(UserId.Parse(AccountTestData.SecondUserId));

        Assert.Throws<InvalidOperationException>(() => new AppSessionState.Unlocked(Account, Substitute.For<IUnlockedVault>(), otherKey, Substitute.For<IServerNotificationsClient>()));
        otherKey.DidNotReceive().Dispose();
    }

    [Fact]
    public async Task Wait_until_unlocked_returns_a_captured_lease_that_survives_lock()
    {
        var store = new AppSessionStore(Substitute.For<FluentBitwarden.Platform.Ipc.Abstractions.IIpcEventPublisher>());
        await store.AuthenticateAsync(Account, CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        Task<IAppSessionAccessor> pending = store.WaitUntilUnlockedAsync(cancellation.Token).AsTask();
        var vault = Substitute.For<IUnlockedVault>();
        var key = Substitute.For<IAccountKeySession>();
        key.UserId.Returns(Account.UserId);
        await store.UnlockAsync(new AppSessionState.Unlocked(Account, vault, key, Substitute.For<IServerNotificationsClient>()));
        IAppSessionAccessor lease = await pending;
        await store.LockAsync(CancellationToken.None);

        Assert.Same(vault, lease.RequireUnlockedState().Vault);
        vault.DidNotReceive().Dispose();
        await lease.DisposeAsync();
        vault.Received(1).Dispose();
        key.Received(1).Dispose();
    }
}
