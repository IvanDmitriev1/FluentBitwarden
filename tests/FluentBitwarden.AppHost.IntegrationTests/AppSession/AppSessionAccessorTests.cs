using BitwardenApi.Notifications.Abstractions;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.AppSession.Services;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;

namespace FluentBitwarden.AppHost.IntegrationTests.AppSession;

public sealed class AppSessionAccessorTests
{
    private static AccountProfile Account => AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");

    [Fact]
    public async Task Accessor_created_while_locked_captures_the_state_when_first_read()
    {
        await using var context = new SessionTestContext();
        await context.Store.AuthenticateAsync(Account, CancellationToken.None);
        await using var accessor = new AppSessionAccessor(context.Store);
        var vault = Substitute.For<IUnlockedVault>();
        var key = CreateKey();
        var state = new AppSessionState.Unlocked(Account, vault, key, Substitute.For<IServerNotificationsDispatcher>());

        await context.Store.UnlockAsync(state);

        Assert.Same(state, accessor.State);

        await accessor.DisposeAsync();
    }

    [Fact]
    public async Task Accessor_keeps_the_captured_state_through_lock_and_reunlock()
    {
        await using var context = new SessionTestContext();
        var (firstState, firstVault, firstKey) = await Unlock(context);
        await using var accessor = new AppSessionAccessor(context.Store);
        Assert.Same(firstState, accessor.State);
        CancellationToken revoked = firstState.Revoked;

        await context.Store.LockAsync(CancellationToken.None);
        var secondVault = Substitute.For<IUnlockedVault>();
        var secondKey = CreateKey();
        var secondState = new AppSessionState.Unlocked(Account, secondVault, secondKey, Substitute.For<IServerNotificationsDispatcher>());
        await context.Store.UnlockAsync(secondState);

        Assert.True(revoked.IsCancellationRequested);
        Assert.Same(firstState, accessor.State);
        Assert.NotSame(secondState, accessor.State);
        firstVault.DidNotReceive().Dispose();
        firstKey.DidNotReceive().Dispose();

        await accessor.DisposeAsync();
        firstVault.Received(1).Dispose();
        firstKey.Received(1).Dispose();
    }

    [Fact]
    public async Task Disposing_an_unused_accessor_does_not_pin_the_unlocked_state()
    {
        await using var context = new SessionTestContext();
        var (_, vault, key) = await Unlock(context);
        await using var accessor = new AppSessionAccessor(context.Store);

        await accessor.DisposeAsync();
        await context.Store.LockAsync(CancellationToken.None);

        vault.Received(1).Dispose();
        key.Received(1).Dispose();
        Assert.Throws<ObjectDisposedException>(() => _ = accessor.State);
    }

    [Fact]
    public async Task Repeated_reads_and_disposal_release_one_reference_once()
    {
        await using var context = new SessionTestContext();
        var (state, vault, key) = await Unlock(context);
        await using var accessor = new AppSessionAccessor(context.Store);
        Assert.Same(state, accessor.State);
        Assert.Same(state, accessor.State);
        await context.Store.LockAsync(CancellationToken.None);
        vault.DidNotReceive().Dispose();

        await accessor.DisposeAsync();
        await accessor.DisposeAsync();

        vault.Received(1).Dispose();
        key.Received(1).Dispose();
    }

    [Fact]
    public async Task Independent_accessors_keep_state_alive_until_both_scopes_release()
    {
        await using var context = new SessionTestContext();
        var (_, vault, key) = await Unlock(context);
        await using var first = new AppSessionAccessor(context.Store);
        await using var second = new AppSessionAccessor(context.Store);
        Assert.Same(first.State, second.State);

        await context.Store.LockAsync(CancellationToken.None);
        await first.DisposeAsync();
        vault.DidNotReceive().Dispose();
        key.DidNotReceive().Dispose();

        await second.DisposeAsync();
        vault.Received(1).Dispose();
        key.Received(1).Dispose();
    }

    private static IAccountKeySession CreateKey()
    {
        IAccountKeySession key = Substitute.For<IAccountKeySession>();
        key.UserId.Returns(Account.UserId);
        return key;
    }

    private static async Task<(AppSessionState.Unlocked State, IUnlockedVault Vault, IAccountKeySession Key)> Unlock(SessionTestContext context)
    {
        var vault = Substitute.For<IUnlockedVault>();
        var key = CreateKey();
        var state = new AppSessionState.Unlocked(Account, vault, key, Substitute.For<IServerNotificationsDispatcher>());
        await context.Store.AuthenticateAsync(Account, CancellationToken.None);
        await context.Store.UnlockAsync(state);
        return (state, vault, key);
    }
}
