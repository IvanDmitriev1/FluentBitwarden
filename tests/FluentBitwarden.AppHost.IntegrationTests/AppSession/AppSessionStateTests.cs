using BitwardenApi.Notifications.Abstractions;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.AppSession.State;
using MemoryPack;

namespace FluentBitwarden.AppHost.IntegrationTests.AppSession;

public sealed class AppSessionStateTests
{
    private static AccountProfile Account => AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");

    [Fact]
    public async Task Snapshot_mapping_covers_each_state_without_exposing_vault_or_key()
    {
        await using var notAuthenticated = new AppSessionState.NotAuthenticated();
        Assert.IsType<AppSessionStateSnapshot.NotAuthenticated>(notAuthenticated.ToSnapshot);

        await using var locked = new AppSessionState.Locked(Account);
        Assert.Equal(Account, Assert.IsType<AppSessionStateSnapshot.Locked>(locked.ToSnapshot).Account);

        var vault = Substitute.For<IUnlockedVault>();
        var key = Substitute.For<IAccountKeySession>();
        key.UserId.Returns(Account.UserId);
        await using var unlocked = new AppSessionState.Unlocked(Account, vault, key, Substitute.For<IServerNotificationsDispatcher>());
        var snapshot = Assert.IsType<AppSessionStateSnapshot.Unlocked>(unlocked.ToSnapshot);
        Assert.Equal(Account, snapshot.Account);
        Assert.DoesNotContain("Vault =", snapshot.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("AccountKeySession", snapshot.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Snapshot_union_variants_round_trip_through_MemoryPack()
    {
        AppSessionStateSnapshot[] states =
        [
            new AppSessionStateSnapshot.NotAuthenticated(),
            new AppSessionStateSnapshot.Locked(Account),
            new AppSessionStateSnapshot.Unlocked(Account)
        ];

        foreach (AppSessionStateSnapshot state in states)
        {
            byte[] bytes = MemoryPackSerializer.Serialize<AppSessionStateSnapshot>(state);
            AppSessionStateSnapshot roundTrip = MemoryPackSerializer.Deserialize<AppSessionStateSnapshot>(bytes)!;
            Assert.Equal(state, roundTrip);
            Assert.Equal(state.GetType(), roundTrip.GetType());
        }
    }

    [Fact]
    public async Task Disposal_revokes_state_and_releases_owned_resources_once()
    {
        var vault = Substitute.For<IUnlockedVault>();
        var key = Substitute.For<IAccountKeySession>();
        key.UserId.Returns(Account.UserId);
        var state = new AppSessionState.Unlocked(Account, vault, key, Substitute.For<IServerNotificationsDispatcher>());
        CancellationToken revoked = state.Revoked;

        await state.DisposeAsync();
        await state.DisposeAsync();

        Assert.True(revoked.IsCancellationRequested);
        vault.Received(1).Dispose();
        key.Received(1).Dispose();
        Assert.Throws<ObjectDisposedException>(() => state.AddReference());
    }

    [Fact]
    public async Task Existing_references_keep_resources_alive_until_the_final_release()
    {
        var vault = Substitute.For<IUnlockedVault>();
        var key = Substitute.For<IAccountKeySession>();
        key.UserId.Returns(Account.UserId);
        var state = new AppSessionState.Unlocked(Account, vault, key, Substitute.For<IServerNotificationsDispatcher>());
        state.AddReference();
        state.AddReference();

        await state.DisposeAsync();
        vault.DidNotReceive().Dispose();
        key.DidNotReceive().Dispose();
        await state.ReleaseAsync();
        vault.DidNotReceive().Dispose();

        await state.ReleaseAsync();
        vault.Received(1).Dispose();
        key.Received(1).Dispose();
    }

    [Fact]
    public async Task Releasing_the_store_owned_reference_without_retiring_state_is_rejected()
    {
        var state = new AppSessionState.Locked(Account);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await state.ReleaseAsync());
        await state.DisposeAsync();
    }

    [Fact]
    public async Task Disposal_awaits_notifications_before_releasing_vault_and_key()
    {
        var vault = Substitute.For<IUnlockedVault>();
        var key = Substitute.For<IAccountKeySession>();
        key.UserId.Returns(Account.UserId);
        var notifications = Substitute.For<IServerNotificationsDispatcher>();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        notifications.DisposeAsync().Returns(_ => new ValueTask(completion.Task));
        var state = new AppSessionState.Unlocked(Account, vault, key, notifications);
        CancellationToken revoked = state.Revoked;

        Task disposal = state.DisposeAsync().AsTask();
        try
        {
            Assert.True(revoked.IsCancellationRequested);
            Assert.False(disposal.IsCompleted);
            vault.DidNotReceive().Dispose();
            key.DidNotReceive().Dispose();
        }
        finally
        {
            completion.SetResult();
            await disposal;
        }

        vault.Received(1).Dispose();
        key.Received(1).Dispose();
        await state.DisposeAsync();
        await notifications.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task Final_lease_release_awaits_notification_disposal()
    {
        var vault = Substitute.For<IUnlockedVault>();
        var key = Substitute.For<IAccountKeySession>();
        key.UserId.Returns(Account.UserId);
        var notifications = Substitute.For<IServerNotificationsDispatcher>();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        notifications.DisposeAsync().Returns(_ => new ValueTask(completion.Task));
        var state = new AppSessionState.Unlocked(Account, vault, key, notifications);
        state.AddReference();

        await state.DisposeAsync();
        await notifications.DidNotReceive().DisposeAsync();
        Task release = state.ReleaseAsync().AsTask();
        try
        {
            Assert.False(release.IsCompleted);
            vault.DidNotReceive().Dispose();
        }
        finally
        {
            completion.SetResult();
            await release;
        }

        vault.Received(1).Dispose();
        key.Received(1).Dispose();
    }

    [Fact]
    public async Task Notification_disposal_failure_still_releases_vault_and_key()
    {
        var vault = Substitute.For<IUnlockedVault>();
        var key = Substitute.For<IAccountKeySession>();
        key.UserId.Returns(Account.UserId);
        var notifications = Substitute.For<IServerNotificationsDispatcher>();
        var failure = new IOException("Notification cleanup failed.");
        notifications.DisposeAsync().Returns(_ => ValueTask.FromException(failure));
        var state = new AppSessionState.Unlocked(Account, vault, key, notifications);

        var exception = await Assert.ThrowsAsync<IOException>(async () => await state.DisposeAsync());

        Assert.Same(failure, exception);
        vault.Received(1).Dispose();
        key.Received(1).Dispose();
    }

    [Fact]
    public void Unlocked_state_rejects_a_key_for_another_account()
    {
        var key = Substitute.For<IAccountKeySession>();
        key.UserId.Returns(UserId.Parse(AccountTestData.SecondUserId));
        Assert.Throws<InvalidOperationException>(() => new AppSessionState.Unlocked(Account, Substitute.For<IUnlockedVault>(), key, Substitute.For<IServerNotificationsDispatcher>()));
        key.DidNotReceive().Dispose();
    }

    [Fact]
    public void Unlocked_state_account_and_key_cannot_be_changed_after_validation()
    {
        var accountProperty = typeof(AppSessionState.Unlocked).GetProperty(nameof(AppSessionState.Unlocked.Account));
        var keyProperty = typeof(AppSessionState.Unlocked).GetProperty(nameof(AppSessionState.Unlocked.AccountKeySession));

        Assert.Null(accountProperty!.SetMethod);
        Assert.Null(keyProperty!.SetMethod);
    }

    [Fact]
    public async Task Unlocked_state_deconstructs_its_validated_references()
    {
        var account = Account;
        var vault = Substitute.For<IUnlockedVault>();
        var key = Substitute.For<IAccountKeySession>();
        key.UserId.Returns(account.UserId);
        var notifications = Substitute.For<IServerNotificationsDispatcher>();
        await using var state = new AppSessionState.Unlocked(account, vault, key, notifications);

        var (deconstructedAccount, deconstructedVault, deconstructedKey, deconstructedNotifications) = state;

        Assert.Same(account, deconstructedAccount);
        Assert.Same(vault, deconstructedVault);
        Assert.Same(key, deconstructedKey);
        Assert.Same(notifications, deconstructedNotifications);
        Assert.Equal(deconstructedAccount.UserId, deconstructedKey.UserId);
    }
}
