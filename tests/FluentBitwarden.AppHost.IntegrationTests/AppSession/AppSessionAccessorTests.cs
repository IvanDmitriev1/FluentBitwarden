using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.AppSession.Services;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.AppSession;

namespace FluentBitwarden.AppHost.IntegrationTests.AppSession;

public sealed class AppSessionAccessorTests
{
    [Fact]
    public void Accessor_captures_the_state_identity_and_revocation_token()
    {
        using var state = new AppSessionState.NotAuthenticated();
        state.AddReference();
        using var accessor = new AppSessionAccessor(state);

        Assert.Same(state, accessor.AppSessionState);
        Assert.Equal(state.Revoked, accessor.Revoked);
        Assert.Throws<UnlockedSessionRequiredException>(() => accessor.RequireUnlockedState());
    }

    [Fact]
    public void RequireUnlockedState_rejects_locked_state()
    {
        using var state = new AppSessionState.Locked(
            AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first"));
        state.AddReference();
        using var accessor = new AppSessionAccessor(state);

        Assert.Throws<UnlockedSessionRequiredException>(() => accessor.RequireUnlockedState());
    }

    [Fact]
    public async Task Disposing_the_last_lease_after_lock_releases_the_vault_and_key()
    {
        var store = new AppSessionStore(Substitute.For<FluentBitwarden.Platform.Ipc.Abstractions.IIpcEventPublisher>());
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        var vault = Substitute.For<IUnlockedVault>();
        var key = Substitute.For<IAccountKeySession>();
        key.UserId.Returns(account.UserId);
        var cancellationToken = TestContext.Current.CancellationToken;
        await store.AuthenticateAsync(account, cancellationToken);
        await store.UnlockAsync(new AppSessionState.Unlocked(account, vault, key));
        var lease = store.CreateLease();

        await store.LockAsync(cancellationToken);

        vault.DidNotReceive().Dispose();
        key.DidNotReceive().Dispose();
        lease.Dispose();
        vault.Received(1).Dispose();
        key.Received(1).Dispose();
    }

    [Fact]
    public async Task Repeated_disposal_does_not_release_the_same_lease_again()
    {
        var store = new AppSessionStore(Substitute.For<FluentBitwarden.Platform.Ipc.Abstractions.IIpcEventPublisher>());
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        var vault = Substitute.For<IUnlockedVault>();
        var key = Substitute.For<IAccountKeySession>();
        key.UserId.Returns(account.UserId);
        var cancellationToken = TestContext.Current.CancellationToken;
        await store.AuthenticateAsync(account, cancellationToken);
        await store.UnlockAsync(new AppSessionState.Unlocked(account, vault, key));
        var lease = store.CreateLease();
        await store.LockAsync(cancellationToken);

        lease.Dispose();
        lease.Dispose();
        lease.Dispose();

        vault.Received(1).Dispose();
        key.Received(1).Dispose();
    }
}
