using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Modules.Vault.Services;
using FluentBitwarden.Contracts.Modules.Vault.Operations;

namespace FluentBitwarden.AppHost.IntegrationTests.Modules.Vault;

public sealed class VaultManagerTests
{
    [Fact]
    public void Open_decrypts_cached_ciphers_and_folders()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var cipher = VaultManagerTestData.Cipher("cached-cipher", keySession.UserKey.Key);
        var folder = VaultManagerTestData.Folder("cached-folder", "Cached folder", keySession.UserKey.Key);
        context.SeedCache([cipher], [folder]);

        using var vault = context.VaultService.Open(context.AccountContext, keySession);

        var openedCipher = Assert.IsType<LoginCipher>(vault.GetCipher(cipher.Id));
        Assert.Equal("Synthetic item", openedCipher.Name);
        Assert.Equal("synthetic-user", openedCipher.Username);
        Assert.Equal("Cached folder", Assert.Single(vault.GetFolders()).Name);
    }

    [Fact]
    public void Open_rejects_a_context_for_another_user_before_reading_the_local_vault()
    {
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var keySession = new TestAccountKeySession(
            new UnlockedUserKey(UserId.Parse(AccountTestData.SecondUserId), [0x10, 0x20, 0x30]));
        var service = new VaultService(null!, null!, null!, null!, null!, null!, null!, null!);

        Assert.Throws<InvalidOperationException>(() =>
            service.Open(account.BitwardenAccountContext, keySession));
    }

    [Fact]
    public async Task Sync_skips_offline_without_calling_the_api_or_changing_cached_data()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var cachedCipher = VaultManagerTestData.Cipher("cached-cipher", keySession.UserKey.Key);
        DateTimeOffset lastRevision = VaultManagerTestData.RevisionDate;
        context.SeedCache([cachedCipher], [VaultManagerTestData.Folder("cached-folder", "Cached folder", keySession.UserKey.Key)], lastRevision);
        context.NetworkStatus.HasInternetAccess.Returns(false);
        using var vault = context.OpenVault();

        context.CreateLease(vault, keySession);
        var result = await context.VaultService.SyncAsync(CancellationToken.None);

        Assert.Equal(VaultSyncResult.SkippedOffline, result);
        _ = context.SyncApi.DidNotReceive().GetRevisionDateAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>());
        Assert.Equal(lastRevision, context.GetServerRevisionDate());
        Assert.Equal("Synthetic item", vault.GetCipher(cachedCipher.Id)?.Name);
        Assert.Equal("Cached folder", Assert.Single(vault.GetFolders()).Name);
        using var cachedVault = context.OpenVault();
        Assert.Equal("Synthetic item", cachedVault.GetCipher(cachedCipher.Id)?.Name);
        Assert.Equal("Cached folder", Assert.Single(cachedVault.GetFolders()).Name);
    }

    [Fact]
    public async Task Sync_returns_no_changes_when_the_server_revision_is_already_cached()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var cachedCipher = VaultManagerTestData.Cipher("cached-cipher", keySession.UserKey.Key);
        DateTimeOffset revision = VaultManagerTestData.RevisionDate;
        context.SeedCache([cachedCipher], [], revision);
        context.SyncApi.GetRevisionDateAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>())
            .Returns(revision);
        using var vault = context.OpenVault();

        context.CreateLease(vault, keySession);
        var result = await context.VaultService.SyncAsync(CancellationToken.None);

        Assert.Equal(VaultSyncResult.NoChanges, result);
        _ = context.SyncApi.DidNotReceive().GetSyncAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>());
        Assert.Equal(revision, context.GetServerRevisionDate());
        Assert.Equal("Synthetic item", vault.GetCipher(cachedCipher.Id)?.Name);
        using var cachedVault = context.OpenVault();
        Assert.Equal("Synthetic item", cachedVault.GetCipher(cachedCipher.Id)?.Name);
    }

    [Fact]
    public async Task Sync_replaces_cached_data_and_commits_the_new_server_revision()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var cachedCipher = VaultManagerTestData.Cipher("cached-cipher", keySession.UserKey.Key);
        var syncedCipher = VaultManagerTestData.Cipher("synced-cipher", keySession.UserKey.Key);
        var syncedFolder = VaultManagerTestData.Folder("synced-folder", "Synced folder", keySession.UserKey.Key);
        DateTimeOffset revision = VaultManagerTestData.RevisionDate.AddDays(1);
        context.SeedCache(
            [cachedCipher],
            [VaultManagerTestData.Folder("cached-folder", "Cached folder", keySession.UserKey.Key)],
            VaultManagerTestData.RevisionDate);
        context.SyncApi.GetRevisionDateAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>())
            .Returns(revision);
        context.SyncApi.GetSyncAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>())
            .Returns(VaultManagerTestData.SyncResponse(context.AccountContext.UserId, [syncedCipher], [syncedFolder]));
        using var vault = context.OpenVault();

        context.CreateLease(vault, keySession);
        var result = await context.VaultService.SyncAsync(CancellationToken.None);

        Assert.Equal(VaultSyncResult.Synced, result);
        await context.SyncApi.Received(1).GetRevisionDateAsync(context.AccountContext, Arg.Any<CancellationToken>());
        await context.SyncApi.Received(1).GetSyncAsync(context.AccountContext, Arg.Any<CancellationToken>());
        Assert.Null(vault.GetCipher(cachedCipher.Id));
        Assert.Equal("Synthetic item", vault.GetCipher(syncedCipher.Id)?.Name);
        var activeFolder = Assert.Single(vault.GetFolders());
        Assert.Equal(syncedFolder.Id, activeFolder.Id);
        Assert.Equal("Synced folder", activeFolder.Name);
        Assert.Equal(revision, context.GetServerRevisionDate());
        using var syncedVault = context.OpenVault();
        Assert.Null(syncedVault.GetCipher(cachedCipher.Id));
        Assert.Equal("Synthetic item", syncedVault.GetCipher(syncedCipher.Id)?.Name);
        var folder = Assert.Single(syncedVault.GetFolders());
        Assert.Equal(syncedFolder.Id, folder.Id);
        Assert.Equal("Synced folder", folder.Name);
    }

    [Fact]
    public async Task Sync_rejects_a_mismatched_profile_without_changing_cached_data()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var cachedCipher = VaultManagerTestData.Cipher("cached-cipher", keySession.UserKey.Key);
        DateTimeOffset lastRevision = VaultManagerTestData.RevisionDate;
        context.SeedCache(
            [cachedCipher],
            [VaultManagerTestData.Folder("cached-folder", "Cached folder", keySession.UserKey.Key)],
            lastRevision);
        context.SyncApi.GetRevisionDateAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>())
            .Returns(lastRevision.AddDays(1));
        context.SyncApi.GetSyncAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>())
            .Returns(VaultManagerTestData.SyncResponse(
                UserId.Parse(AccountTestData.SecondUserId), [], []));
        using var vault = context.OpenVault();

        context.CreateLease(vault, keySession);
        var result = await context.VaultService.SyncAsync(CancellationToken.None);

        Assert.Equal(VaultSyncResult.Failed, result);
        Assert.Equal(lastRevision, context.GetServerRevisionDate());
        Assert.Equal("Synthetic item", vault.GetCipher(cachedCipher.Id)?.Name);
        Assert.Equal("Cached folder", Assert.Single(vault.GetFolders()).Name);
        using var cachedVault = context.OpenVault();
        Assert.Equal("Synthetic item", cachedVault.GetCipher(cachedCipher.Id)?.Name);
        Assert.Equal("Cached folder", Assert.Single(cachedVault.GetFolders()).Name);
    }

    [Fact]
    public void Session_rejects_a_key_for_another_account_without_calling_the_api_or_changing_live_data()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var cachedCipher = VaultManagerTestData.Cipher("cached-cipher", keySession.UserKey.Key);
        context.SeedCache([cachedCipher]);
        using var vault = context.OpenVault();
        using var mismatchedKeySession = new TestAccountKeySession(
            new UnlockedUserKey(UserId.Parse(AccountTestData.SecondUserId), [0x10, 0x20, 0x30]));

        Assert.Throws<InvalidOperationException>(() => context.CreateLease(vault, mismatchedKeySession));
        _ = context.SyncApi.DidNotReceive().GetRevisionDateAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>());
        Assert.Equal("Synthetic item", vault.GetCipher(cachedCipher.Id)?.Name);
    }

    [Fact]
    public async Task Sync_keeps_live_data_when_the_encrypted_response_is_malformed()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var cachedCipher = VaultManagerTestData.Cipher("cached-cipher", keySession.UserKey.Key);
        var malformedCipher = VaultManagerTestData.Cipher("malformed-cipher", keySession.UserKey.Key);
        malformedCipher.Data[0] = (byte)'x';
        DateTimeOffset lastRevision = VaultManagerTestData.RevisionDate;
        context.SeedCache([cachedCipher], [], lastRevision);
        context.SyncApi.GetRevisionDateAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>())
            .Returns(lastRevision.AddDays(1));
        context.SyncApi.GetSyncAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>())
            .Returns(VaultManagerTestData.SyncResponse(context.AccountContext.UserId, [malformedCipher], []));
        using var vault = context.OpenVault();

        context.CreateLease(vault, keySession);
        var result = await context.VaultService.SyncAsync(CancellationToken.None);

        Assert.Equal(VaultSyncResult.Failed, result);
        Assert.Equal(lastRevision, context.GetServerRevisionDate());
        Assert.Equal("Synthetic item", vault.GetCipher(cachedCipher.Id)?.Name);
        Assert.Null(vault.GetCipher(malformedCipher.Id));
    }

    [Fact]
    public async Task Sync_keeps_cached_data_when_the_api_fails()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var cachedCipher = VaultManagerTestData.Cipher("cached-cipher", keySession.UserKey.Key);
        DateTimeOffset lastRevision = VaultManagerTestData.RevisionDate;
        context.SeedCache(
            [cachedCipher],
            [VaultManagerTestData.Folder("cached-folder", "Cached folder", keySession.UserKey.Key)],
            lastRevision);
        context.SyncApi.GetRevisionDateAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>())
            .Returns(lastRevision.AddDays(1));
        context.SyncApi.GetSyncAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<VaultSyncResponse>(new InvalidOperationException("Sync failed.")));
        using var vault = context.OpenVault();

        context.CreateLease(vault, keySession);
        var result = await context.VaultService.SyncAsync(CancellationToken.None);

        Assert.Equal(VaultSyncResult.Failed, result);
        Assert.Equal(lastRevision, context.GetServerRevisionDate());
        Assert.Equal("Synthetic item", vault.GetCipher(cachedCipher.Id)?.Name);
        Assert.Equal("Cached folder", Assert.Single(vault.GetFolders()).Name);
        using var cachedVault = context.OpenVault();
        Assert.Equal("Synthetic item", cachedVault.GetCipher(cachedCipher.Id)?.Name);
        Assert.Equal("Cached folder", Assert.Single(cachedVault.GetFolders()).Name);
    }

    [Fact]
    public async Task Sync_returns_skipped_offline_when_a_request_is_canceled_without_changing_cache()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var cachedCipher = VaultManagerTestData.Cipher("cached-cipher", keySession.UserKey.Key);
        DateTimeOffset lastRevision = VaultManagerTestData.RevisionDate;
        context.SeedCache([cachedCipher], [], lastRevision);
        context.SyncApi.GetRevisionDateAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>())
            .Returns(lastRevision.AddDays(1));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        context.SyncApi.GetSyncAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromCanceled<VaultSyncResponse>(cancellation.Token));
        using var vault = context.OpenVault();

        context.CreateLease(vault, keySession);
        var result = await context.VaultService.SyncAsync(cancellation.Token);

        Assert.Equal(VaultSyncResult.SkippedOffline, result);
        Assert.Equal(lastRevision, context.GetServerRevisionDate());
        Assert.Equal("Synthetic item", vault.GetCipher(cachedCipher.Id)?.Name);
        using var cachedVault = context.OpenVault();
        Assert.Equal("Synthetic item", cachedVault.GetCipher(cachedCipher.Id)?.Name);
    }

    [Fact]
    public async Task SaveCipher_creates_a_cipher_and_persists_the_decrypted_server_response()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var existingCipher = VaultManagerTestData.Cipher("existing-cipher", keySession.UserKey.Key);
        var savedResponse = VaultManagerTestData.Cipher("created-cipher", keySession.UserKey.Key);
        context.SeedCache([existingCipher], []);
        context.CiphersApi.CreateCipherAsync(
                Arg.Any<BitwardenAccountContext>(),
                Arg.Any<CipherRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(savedResponse);
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Name = "Unsaved input";
        cipher.Username = "input-user";
        using var vault = context.OpenVault();

        context.CreateLease(vault, keySession);
        var saved = await context.VaultService.SaveCipherAsync(cipher, CancellationToken.None);

        var savedLogin = Assert.IsType<LoginCipher>(saved);
        Assert.Equal(savedResponse.Id, savedLogin.Id);
        Assert.Equal("Synthetic item", savedLogin.Name);
        Assert.Equal("synthetic-user", savedLogin.Username);
        await context.CiphersApi.Received(1).CreateCipherAsync(
            context.AccountContext,
            Arg.Any<CipherRequest>(),
            Arg.Any<CancellationToken>());
        _ = context.CiphersApi.DidNotReceive().UpdateCipherAsync(
            Arg.Any<BitwardenAccountContext>(),
            Arg.Any<CipherId>(),
            Arg.Any<CipherRequest>(),
            Arg.Any<CancellationToken>());

        Assert.Equal("Synthetic item", vault.GetCipher(savedResponse.Id)?.Name);
        Assert.Equal("Synthetic item", vault.GetCipher(existingCipher.Id)?.Name);
        using var cachedVault = context.OpenVault();
        Assert.Equal("Synthetic item", cachedVault.GetCipher(savedResponse.Id)?.Name);
        Assert.Equal("Synthetic item", cachedVault.GetCipher(existingCipher.Id)?.Name);
    }

    [Fact]
    public async Task SaveCipher_updates_an_existing_cipher()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var existingCipher = VaultManagerTestData.Cipher("existing-cipher", keySession.UserKey.Key);
        var otherCipher = VaultManagerTestData.Cipher("other-cipher", keySession.UserKey.Key, "Unchanged item");
        var updatedResponse = VaultManagerTestData.Cipher("existing-cipher", keySession.UserKey.Key, "Updated item");
        context.SeedCache([existingCipher, otherCipher], []);
        context.CiphersApi.UpdateCipherAsync(
                Arg.Any<BitwardenAccountContext>(),
                existingCipher.Id,
                Arg.Any<CipherRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(updatedResponse);
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = existingCipher.Id;
        cipher.Name = "Edited input";
        using var vault = context.OpenVault();

        context.CreateLease(vault, keySession);
        var saved = await context.VaultService.SaveCipherAsync(cipher, CancellationToken.None);

        Assert.Equal(existingCipher.Id, saved.Id);
        Assert.Equal("Updated item", saved.Name);
        await context.CiphersApi.Received(1).UpdateCipherAsync(
            context.AccountContext,
            existingCipher.Id,
            Arg.Any<CipherRequest>(),
            Arg.Any<CancellationToken>());
        _ = context.CiphersApi.DidNotReceive().CreateCipherAsync(
            Arg.Any<BitwardenAccountContext>(),
            Arg.Any<CipherRequest>(),
            Arg.Any<CancellationToken>());

        Assert.Equal("Updated item", vault.GetCipher(existingCipher.Id)?.Name);
        Assert.Equal("Unchanged item", vault.GetCipher(otherCipher.Id)?.Name);
        using var cachedVault = context.OpenVault();
        Assert.Equal("Updated item", cachedVault.GetCipher(existingCipher.Id)?.Name);
        Assert.Equal("Unchanged item", cachedVault.GetCipher(otherCipher.Id)?.Name);
    }

    [Fact]
    public async Task SaveCipher_keeps_cached_data_when_the_api_fails()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var existingCipher = VaultManagerTestData.Cipher("existing-cipher", keySession.UserKey.Key);
        context.SeedCache(
            [existingCipher],
            [VaultManagerTestData.Folder("cached-folder", "Cached folder", keySession.UserKey.Key)]);
        context.CiphersApi.UpdateCipherAsync(
                Arg.Any<BitwardenAccountContext>(),
                existingCipher.Id,
                Arg.Any<CipherRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CipherResponse>(new InvalidOperationException("Save failed.")));
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = existingCipher.Id;
        cipher.Name = "Edited input";
        using var vault = context.OpenVault();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateAndSave(context, vault, keySession, cipher, CancellationToken.None));

        Assert.Equal("Synthetic item", vault.GetCipher(existingCipher.Id)?.Name);
        Assert.Equal("Cached folder", Assert.Single(vault.GetFolders()).Name);
        using var cachedVault = context.OpenVault();
        Assert.Equal("Synthetic item", cachedVault.GetCipher(existingCipher.Id)?.Name);
        Assert.Equal("Cached folder", Assert.Single(cachedVault.GetFolders()).Name);
    }

    [Fact]
    public async Task SaveCipher_rejects_a_key_for_another_account_without_calling_the_api()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var existingCipher = VaultManagerTestData.Cipher("existing-cipher", keySession.UserKey.Key);
        context.SeedCache([existingCipher]);
        using var vault = context.OpenVault();
        using var mismatchedKeySession = new TestAccountKeySession(
            new UnlockedUserKey(UserId.Parse(AccountTestData.SecondUserId), [0x10, 0x20, 0x30]));
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateAndSave(context, vault, mismatchedKeySession, cipher, CancellationToken.None));

        _ = context.CiphersApi.DidNotReceive().CreateCipherAsync(
            Arg.Any<BitwardenAccountContext>(), Arg.Any<CipherRequest>(), Arg.Any<CancellationToken>());
        _ = context.CiphersApi.DidNotReceive().UpdateCipherAsync(
            Arg.Any<BitwardenAccountContext>(), Arg.Any<CipherId>(), Arg.Any<CipherRequest>(), Arg.Any<CancellationToken>());
        Assert.Equal("Synthetic item", vault.GetCipher(existingCipher.Id)?.Name);
    }

    [Fact]
    public async Task SaveCipher_keeps_live_data_when_the_encrypted_response_is_malformed()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var existingCipher = VaultManagerTestData.Cipher("existing-cipher", keySession.UserKey.Key);
        var malformedCipher = VaultManagerTestData.Cipher("existing-cipher", keySession.UserKey.Key);
        malformedCipher.Data[0] = (byte)'x';
        context.SeedCache([existingCipher]);
        context.CiphersApi.UpdateCipherAsync(
                Arg.Any<BitwardenAccountContext>(),
                existingCipher.Id,
                Arg.Any<CipherRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(malformedCipher);
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = existingCipher.Id;
        using var vault = context.OpenVault();

        await Assert.ThrowsAnyAsync<System.Text.Json.JsonException>(() =>
            CreateAndSave(context, vault, keySession, cipher, CancellationToken.None));

        Assert.Equal("Synthetic item", vault.GetCipher(existingCipher.Id)?.Name);
        using var persistedVault = context.OpenVault();
        Assert.Equal("Synthetic item", persistedVault.GetCipher(existingCipher.Id)?.Name);
    }
    private static Task<Cipher> CreateAndSave(VaultManagerTestContext context, FluentBitwarden.AppHost.Modules.Vault.Contracts.IUnlockedVault vault,
        FluentBitwarden.AppHost.Modules.Account.Contracts.IAccountKeySession keySession, Cipher cipher, CancellationToken cancellationToken)
    {
        context.CreateLease(vault, keySession);
        return context.VaultService.SaveCipherAsync(cipher, cancellationToken);
    }
}
