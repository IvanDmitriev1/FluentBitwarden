using BitwardenApi.Vault.Cryptography;
using BitwardenApi.Vault.Items;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Persistence;
using FluentBitwarden.AppHost.Modules.Vault.Services;
using FluentBitwarden.Contracts.Modules.Vault.Operations;
using FluentBitwarden.Platform.Infrastructure.Connectivity;
using NSubstitute;
using static FluentBitwarden.AppHost.IntegrationTests.Modules.Vault.VaultParserTestData;

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

        var openedCipher = Assert.IsType<LoginVaultCipher>(vault.GetCipher(cipher.Id));
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
        var service = new VaultService(null!, null!, null!, null!, null!, null!);

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

        var result = await context.VaultService.SyncAsync(vault, keySession, CancellationToken.None);

        Assert.Equal(VaultSyncResult.SkippedOffline, result);
        _ = context.Api.DidNotReceive().GetRevisionDateAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>());
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
        context.Api.GetRevisionDateAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>())
            .Returns(revision);
        using var vault = context.OpenVault();

        var result = await context.VaultService.SyncAsync(vault, keySession, CancellationToken.None);

        Assert.Equal(VaultSyncResult.NoChanges, result);
        _ = context.Api.DidNotReceive().GetSyncAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>());
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
        context.Api.GetRevisionDateAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>())
            .Returns(revision);
        context.Api.GetSyncAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>())
            .Returns(VaultManagerTestData.SyncResponse(context.AccountContext.UserId, [syncedCipher], [syncedFolder]));
        using var vault = context.OpenVault();

        var result = await context.VaultService.SyncAsync(vault, keySession, CancellationToken.None);

        Assert.Equal(VaultSyncResult.Synced, result);
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
        context.Api.GetRevisionDateAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>())
            .Returns(lastRevision.AddDays(1));
        context.Api.GetSyncAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>())
            .Returns(VaultManagerTestData.SyncResponse(
                UserId.Parse(AccountTestData.SecondUserId), [], []));
        using var vault = context.OpenVault();

        var result = await context.VaultService.SyncAsync(vault, keySession, CancellationToken.None);

        Assert.Equal(VaultSyncResult.Failed, result);
        Assert.Equal(lastRevision, context.GetServerRevisionDate());
        Assert.Equal("Synthetic item", vault.GetCipher(cachedCipher.Id)?.Name);
        Assert.Equal("Cached folder", Assert.Single(vault.GetFolders()).Name);
        using var cachedVault = context.OpenVault();
        Assert.Equal("Synthetic item", cachedVault.GetCipher(cachedCipher.Id)?.Name);
        Assert.Equal("Cached folder", Assert.Single(cachedVault.GetFolders()).Name);
    }

    [Fact]
    public async Task Sync_rejects_a_key_for_another_account_without_calling_the_api_or_changing_live_data()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var cachedCipher = VaultManagerTestData.Cipher("cached-cipher", keySession.UserKey.Key);
        context.SeedCache([cachedCipher]);
        using var vault = context.OpenVault();
        using var mismatchedKeySession = new TestAccountKeySession(
            new UnlockedUserKey(UserId.Parse(AccountTestData.SecondUserId), [0x10, 0x20, 0x30]));

        var result = await context.VaultService.SyncAsync(vault, mismatchedKeySession, CancellationToken.None);

        Assert.Equal(VaultSyncResult.Failed, result);
        _ = context.Api.DidNotReceive().GetRevisionDateAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>());
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
        context.Api.GetRevisionDateAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>())
            .Returns(lastRevision.AddDays(1));
        context.Api.GetSyncAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>())
            .Returns(VaultManagerTestData.SyncResponse(context.AccountContext.UserId, [malformedCipher], []));
        using var vault = context.OpenVault();

        var result = await context.VaultService.SyncAsync(vault, keySession, CancellationToken.None);

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
        context.Api.GetRevisionDateAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>())
            .Returns(lastRevision.AddDays(1));
        context.Api.GetSyncAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<VaultSyncResponse>(new InvalidOperationException("Sync failed.")));
        using var vault = context.OpenVault();

        var result = await context.VaultService.SyncAsync(vault, keySession, CancellationToken.None);

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
        context.Api.GetRevisionDateAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>())
            .Returns(lastRevision.AddDays(1));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        context.Api.GetSyncAsync(Arg.Any<BitwardenAccountContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromCanceled<VaultSyncResponse>(cancellation.Token));
        using var vault = context.OpenVault();

        var result = await context.VaultService.SyncAsync(vault, keySession, cancellation.Token);

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
        context.Api.CreateCipherAsync(
                Arg.Any<BitwardenAccountContext>(),
                Arg.Any<VaultCipherRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(savedResponse);
        var cipher = (LoginVaultCipher)VaultCipher.CreateBlankCipher(VaultCipherType.Login);
        cipher.Name = "Unsaved input";
        cipher.Username = "input-user";
        using var vault = context.OpenVault();

        var saved = await context.VaultService.SaveCipherAsync(vault, keySession, cipher, CancellationToken.None);

        var savedLogin = Assert.IsType<LoginVaultCipher>(saved);
        Assert.Equal(savedResponse.Id, savedLogin.Id);
        Assert.Equal("Synthetic item", savedLogin.Name);
        Assert.Equal("synthetic-user", savedLogin.Username);
        await context.Api.Received(1).CreateCipherAsync(
            context.AccountContext,
            Arg.Any<VaultCipherRequest>(),
            Arg.Any<CancellationToken>());
        _ = context.Api.DidNotReceive().UpdateCipherAsync(
            Arg.Any<BitwardenAccountContext>(),
            Arg.Any<CipherId>(),
            Arg.Any<VaultCipherRequest>(),
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
        context.Api.UpdateCipherAsync(
                Arg.Any<BitwardenAccountContext>(),
                existingCipher.Id,
                Arg.Any<VaultCipherRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(updatedResponse);
        var cipher = (LoginVaultCipher)VaultCipher.CreateBlankCipher(VaultCipherType.Login);
        cipher.Id = existingCipher.Id;
        cipher.Name = "Edited input";
        using var vault = context.OpenVault();

        var saved = await context.VaultService.SaveCipherAsync(vault, keySession, cipher, CancellationToken.None);

        Assert.Equal(existingCipher.Id, saved.Id);
        Assert.Equal("Updated item", saved.Name);
        await context.Api.Received(1).UpdateCipherAsync(
            context.AccountContext,
            existingCipher.Id,
            Arg.Any<VaultCipherRequest>(),
            Arg.Any<CancellationToken>());
        _ = context.Api.DidNotReceive().CreateCipherAsync(
            Arg.Any<BitwardenAccountContext>(),
            Arg.Any<VaultCipherRequest>(),
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
        context.Api.UpdateCipherAsync(
                Arg.Any<BitwardenAccountContext>(),
                existingCipher.Id,
                Arg.Any<VaultCipherRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<VaultCipherResponse>(new InvalidOperationException("Save failed.")));
        var cipher = (LoginVaultCipher)VaultCipher.CreateBlankCipher(VaultCipherType.Login);
        cipher.Id = existingCipher.Id;
        cipher.Name = "Edited input";
        using var vault = context.OpenVault();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.VaultService.SaveCipherAsync(vault, keySession, cipher, CancellationToken.None));

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
        var cipher = (LoginVaultCipher)VaultCipher.CreateBlankCipher(VaultCipherType.Login);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.VaultService.SaveCipherAsync(vault, mismatchedKeySession, cipher, CancellationToken.None));

        _ = context.Api.DidNotReceive().CreateCipherAsync(
            Arg.Any<BitwardenAccountContext>(), Arg.Any<VaultCipherRequest>(), Arg.Any<CancellationToken>());
        _ = context.Api.DidNotReceive().UpdateCipherAsync(
            Arg.Any<BitwardenAccountContext>(), Arg.Any<CipherId>(), Arg.Any<VaultCipherRequest>(), Arg.Any<CancellationToken>());
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
        context.Api.UpdateCipherAsync(
                Arg.Any<BitwardenAccountContext>(),
                existingCipher.Id,
                Arg.Any<VaultCipherRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(malformedCipher);
        var cipher = (LoginVaultCipher)VaultCipher.CreateBlankCipher(VaultCipherType.Login);
        cipher.Id = existingCipher.Id;
        using var vault = context.OpenVault();

        await Assert.ThrowsAnyAsync<System.Text.Json.JsonException>(() =>
            context.VaultService.SaveCipherAsync(vault, keySession, cipher, CancellationToken.None));

        Assert.Equal("Synthetic item", vault.GetCipher(existingCipher.Id)?.Name);
        using var persistedVault = context.OpenVault();
        Assert.Equal("Synthetic item", persistedVault.GetCipher(existingCipher.Id)?.Name);
    }
}

internal sealed class VaultManagerTestContext : IDisposable
{
    private readonly AccountRepositoryTestDatabase _database = new();
    private readonly UnitOfWork _unitOfWork;
    private readonly VaultWriterRepository _vaultWriterRepository;
    private readonly VaultSyncStateRepository _vaultSyncStateRepository;
    public VaultManagerTestContext()
    {
        Account = AccountTestData.Profile(AccountTestData.FirstUserId, "vault@example.test", "first");
        AccountRepositoryTestHelper.InsertAccount(_database, Account);
        _unitOfWork = _database.CreateUnitOfWork();
        _vaultWriterRepository = new VaultWriterRepository(_unitOfWork);
        _vaultSyncStateRepository = new VaultSyncStateRepository(_unitOfWork);
        Api = Substitute.For<IVaultItemsApi>();
        NetworkStatus = Substitute.For<INetworkStatus>();
        NetworkStatus.HasInternetAccess.Returns(true);

        var vaultReaderRepository = new VaultReaderRepository(_unitOfWork);
        VaultService = new VaultService(
            vaultReaderRepository,
            _vaultWriterRepository,
            _vaultSyncStateRepository,
            Api,
            _unitOfWork,
            NetworkStatus);
    }

    public AccountProfile Account { get; }
    public BitwardenAccountContext AccountContext => Account.BitwardenAccountContext;
    public IVaultItemsApi Api { get; }
    public INetworkStatus NetworkStatus { get; }
    public VaultService VaultService { get; }

    public VaultManagerTestKeySession CreateKeySession() => new(AccountContext.UserId);

    public IUnlockedVault OpenVault()
    {
        using var keySession = CreateKeySession();
        return VaultService.Open(AccountContext, keySession);
    }

    public void SeedCache(
        VaultCipherResponse[]? ciphers = null,
        VaultFolderResponse[]? folders = null,
        DateTimeOffset? serverRevisionDate = null)
    {
        _unitOfWork.Begin();
        _vaultWriterRepository.WriteOrganizations(AccountContext.UserId, []);
        _vaultWriterRepository.WriteFolders(AccountContext.UserId, folders ?? []);
        _vaultWriterRepository.WriteCollections(AccountContext.UserId, []);
        _vaultWriterRepository.WriteCiphers(AccountContext.UserId, ciphers ?? []);
        if (serverRevisionDate is { } revisionDate)
            _vaultSyncStateRepository.UpsertServerRevisionDate(AccountContext.UserId, revisionDate);
        _unitOfWork.Commit();
    }

    public DateTimeOffset? GetServerRevisionDate() =>
        _vaultSyncStateRepository.GetServerRevisionDate(AccountContext.UserId);

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _database.Dispose();
    }
}

internal sealed class VaultManagerTestKeySession(UserId userId) : IAccountKeySession
{
    private static readonly byte[] KeyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();

    public UserId UserId => UserKey.UserId;
    public UnlockedUserKey UserKey { get; } = new(userId, (byte[])KeyBytes.Clone());

    public SymmetricCryptoKey GetOrganizationKey(
        OrganizationId organizationId,
        AsymmetricEncString protectedOrganizationKey)
    {
        if (!organizationId.IsEmpty)
            throw new NotSupportedException("Organization keys are not part of these tests.");

        return UserKey;
    }

    public AttachmentKey CreateAttachmentKey(
        AsymmetricEncString protectedOrganizationKey,
        EncString protectedCipherKey,
        EncString protectedAttachmentKey) =>
        throw new NotSupportedException("Attachments are not part of these tests.");

    public void Dispose() => UserKey.Dispose();
}

internal static class VaultManagerTestData
{
    public static readonly DateTimeOffset RevisionDate = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    public static VaultCipherResponse Cipher(
        string id,
        ReadOnlySpan<byte> userKey,
        string name = "Synthetic item")
    {
        byte[] payload = WritePayload(userKey.ToArray(), (writer, key) =>
        {
            writer.WriteStartObject();
            WriteEncrypted(writer, "name", name, key);
            WriteEncrypted(writer, "username", "synthetic-user", key);
            WriteEncrypted(writer, "password", "synthetic-password", key);
            writer.WriteEndObject();
        });
        var cipher = CreateDto(VaultCipherType.Login, payload);
        return cipher with { Id = CipherId.Parse(id), FolderId = FolderId.Empty };
    }

    public static VaultFolderResponse Folder(string id, string name, ReadOnlySpan<byte> userKey) => new()
    {
        Id = FolderId.Parse(id),
        RevisionDate = RevisionDate,
        EncryptedName = EncString.Encrypt(name, userKey)
    };

    public static VaultSyncResponse SyncResponse(
        UserId userId,
        VaultCipherResponse[] ciphers,
        VaultFolderResponse[] folders) => new()
        {
            Profile = AccountTestData.SyncedProfile(
                userId.ToString(),
                "synced@example.test",
                "Synced User",
                "en-US",
                AccountTestData.ProfileCreationDate),
            VaultCiphers = ciphers,
            Folders = folders
        };
}
