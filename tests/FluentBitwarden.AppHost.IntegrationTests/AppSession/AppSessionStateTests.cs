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
    public void Snapshot_mapping_covers_each_state_without_exposing_vault_or_key()
    {
        using var notAuthenticated = new AppSessionState.NotAuthenticated();
        Assert.IsType<AppSessionStateSnapshot.NotAuthenticated>(notAuthenticated.ToSnapshot);

        using var locked = new AppSessionState.Locked(Account);
        Assert.Equal(Account, Assert.IsType<AppSessionStateSnapshot.Locked>(locked.ToSnapshot).Account);

        var vault = Substitute.For<IUnlockedVault>();
        var key = Substitute.For<IAccountKeySession>();
        key.UserId.Returns(Account.UserId);
        using var unlocked = new AppSessionState.Unlocked(Account, vault, key);
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
    public void Disposal_revokes_state_and_releases_owned_resources_once()
    {
        var vault = Substitute.For<IUnlockedVault>();
        var key = Substitute.For<IAccountKeySession>();
        key.UserId.Returns(Account.UserId);
        var state = new AppSessionState.Unlocked(Account, vault, key);
        CancellationToken revoked = state.Revoked;

        state.Dispose();
        state.Dispose();

        Assert.True(revoked.IsCancellationRequested);
        vault.Received(1).Dispose();
        key.Received(1).Dispose();
        Assert.Throws<ObjectDisposedException>(() => state.AddReference());
    }

    [Fact]
    public void Existing_references_keep_resources_alive_until_the_final_release()
    {
        var vault = Substitute.For<IUnlockedVault>();
        var key = Substitute.For<IAccountKeySession>();
        key.UserId.Returns(Account.UserId);
        var state = new AppSessionState.Unlocked(Account, vault, key);
        state.AddReference();
        state.AddReference();

        state.Dispose();
        vault.DidNotReceive().Dispose();
        key.DidNotReceive().Dispose();
        state.Release();
        vault.DidNotReceive().Dispose();

        state.Release();
        vault.Received(1).Dispose();
        key.Received(1).Dispose();
    }

    [Fact]
    public void Releasing_the_store_owned_reference_without_retiring_state_is_rejected()
    {
        var state = new AppSessionState.Locked(Account);
        Assert.Throws<InvalidOperationException>(state.Release);
        state.Dispose();
    }

    [Fact]
    public void Unlocked_state_rejects_a_key_for_another_account()
    {
        var key = Substitute.For<IAccountKeySession>();
        key.UserId.Returns(UserId.Parse(AccountTestData.SecondUserId));
        Assert.Throws<InvalidOperationException>(() => new AppSessionState.Unlocked(Account, Substitute.For<IUnlockedVault>(), key));
        key.DidNotReceive().Dispose();
    }
}
