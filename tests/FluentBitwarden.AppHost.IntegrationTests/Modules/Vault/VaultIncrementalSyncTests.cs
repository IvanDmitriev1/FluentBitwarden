using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.Modules.Vault.Events;

namespace FluentBitwarden.AppHost.IntegrationTests.Modules.Vault;

public sealed class VaultIncrementalSyncTests
{
    [Fact]
    public async Task SyncCipher_inserts_a_missing_cipher()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var response = VaultManagerTestData.Cipher("missing-cipher", keySession.UserKey.Key, "inserted item");
        var folder = VaultManagerTestData.Folder("preserved-folder", "Preserved", keySession.UserKey.Key);
        var unrelated = VaultManagerTestData.Cipher("unrelated-cipher", keySession.UserKey.Key, "Unchanged") with { FolderId = folder.Id };
        context.SeedCache([unrelated], [folder]);
        context.CiphersApi.GetCipherAsync(context.AccountContext, response.Id, Arg.Any<CancellationToken>()).Returns(response);
        using var vault = context.OpenVault();
        context.CreateLease(vault, keySession);
        using var cancellation = new CancellationTokenSource();
        await context.VaultService.SyncCipherAsync(response.Id, cancellation.Token);
        await context.CiphersApi.Received(1).GetCipherAsync(context.AccountContext, response.Id, cancellation.Token);

        Assert.Equal("inserted item", vault.GetCipher(response.Id)?.Name);
        using var persisted = context.OpenVault();
        Assert.Equal("inserted item", persisted.GetCipher(response.Id)?.Name);
        Assert.Equal("Unchanged", persisted.GetCipher(unrelated.Id)?.Name);
        Assert.Equal(folder.Id, persisted.GetCipher(unrelated.Id)?.FolderId);
        Assert.Equal(folder.Id, vault.GetCipher(unrelated.Id)?.FolderId);
        await context.EventPublisher.Received(1).PublishAsync(Arg.Is<CipherChangedEvent>(e =>
            e.UserId == context.Account.UserId && e.Cipher.Id == response.Id &&
            e.Cipher.RevisionDate == response.RevisionDate && e.Cipher.Name == "inserted item"), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(-1, "old")]
    [InlineData(0, "current")]
    [InlineData(1, "new")]
    public async Task SyncCipher_applies_only_a_newer_revision(
        int revisionOffset, string expectedName)
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var cached = VaultManagerTestData.Cipher("incremental-cipher", keySession.UserKey.Key) with
        {
            RevisionDate = VaultManagerTestData.RevisionDate
        };
        var response = VaultManagerTestData.Cipher("incremental-cipher", keySession.UserKey.Key, expectedName) with
        {
            RevisionDate = VaultManagerTestData.RevisionDate.AddDays(revisionOffset)
        };
        context.SeedCache([cached]);
        context.CiphersApi.GetCipherAsync(context.AccountContext, response.Id, Arg.Any<CancellationToken>())
            .Returns(response);
        using var vault = context.OpenVault();
        context.CreateLease(vault, keySession);
        if (revisionOffset > 0)
        {
            context.EventPublisher.When(p => p.PublishAsync(Arg.Any<CipherChangedEvent>(), Arg.Any<CancellationToken>()))
                .Do(_ =>
                {
                    Assert.Equal(expectedName, vault.GetCipher(response.Id)?.Name);
                    using var committed = context.OpenVaultFromFreshConnection(keySession);
                    Assert.Equal(expectedName, committed.GetCipher(response.Id)?.Name);
                });
        }

        await context.VaultService.SyncCipherAsync(response.Id, CancellationToken.None);

        string expected = revisionOffset > 0 ? expectedName : "Synthetic item";
        Assert.Equal(expected, vault.GetCipher(response.Id)?.Name);
        using var persisted = context.OpenVault();
        Assert.Equal(expected, persisted.GetCipher(response.Id)?.Name);
        Assert.Equal(revisionOffset > 0 ? response.RevisionDate : null, context.GetServerRevisionDate());
        if (revisionOffset > 0)
        {
            await context.EventPublisher.Received(1).PublishAsync(Arg.Is<CipherChangedEvent>(e =>
                e.UserId == context.Account.UserId && e.Cipher.Id == response.Id &&
                e.Cipher.RevisionDate == response.RevisionDate && e.Cipher.Name == expected), Arg.Any<CancellationToken>());
        }
        else
        {
            Assert.Empty(context.EventPublisher.ReceivedCalls());
        }
    }

    [Fact]
    public async Task SyncCipher_advances_checkpoint_to_applied_revision()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var checkpoint = VaultManagerTestData.RevisionDate.AddDays(-10);
        context.SeedCache(serverRevisionDate: checkpoint);
        var response = VaultManagerTestData.Cipher("checkpoint-cipher", keySession.UserKey.Key) with { RevisionDate = VaultManagerTestData.RevisionDate.AddDays(1) };
        context.CiphersApi.GetCipherAsync(context.AccountContext, response.Id, Arg.Any<CancellationToken>()).Returns(response);
        using var vault = context.OpenVault();
        context.CreateLease(vault, keySession);

        await context.VaultService.SyncCipherAsync(response.Id, CancellationToken.None);

        Assert.Equal(response.RevisionDate, context.GetServerRevisionDate());
    }

    [Fact]
    public async Task SyncCipher_keeps_checkpoint_monotonic_for_out_of_order_responses()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var later = VaultManagerTestData.Cipher("later-cipher", keySession.UserKey.Key, "later") with
        {
            RevisionDate = VaultManagerTestData.RevisionDate.AddDays(2)
        };
        var earlier = VaultManagerTestData.Cipher("earlier-cipher", keySession.UserKey.Key, "earlier") with
        {
            RevisionDate = VaultManagerTestData.RevisionDate.AddDays(1)
        };
        context.CiphersApi.GetCipherAsync(context.AccountContext, later.Id, Arg.Any<CancellationToken>()).Returns(later);
        context.CiphersApi.GetCipherAsync(context.AccountContext, earlier.Id, Arg.Any<CancellationToken>()).Returns(earlier);
        using var vault = context.OpenVault();
        context.CreateLease(vault, keySession);

        await context.VaultService.SyncCipherAsync(later.Id, CancellationToken.None);
        await context.VaultService.SyncCipherAsync(earlier.Id, CancellationToken.None);

        Assert.Equal("later", vault.GetCipher(later.Id)?.Name);
        Assert.Equal("earlier", vault.GetCipher(earlier.Id)?.Name);
        Assert.Equal(later.RevisionDate, context.GetServerRevisionDate());
    }

    [Fact]
    public async Task SyncCipher_persistence_failure_rolls_back_item_and_checkpoint()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var checkpoint = VaultManagerTestData.RevisionDate;
        var cached = VaultManagerTestData.Cipher("persistence-failure", keySession.UserKey.Key, "original value");
        context.SeedCache([cached], serverRevisionDate: checkpoint);
        var response = VaultManagerTestData.Cipher("persistence-failure", keySession.UserKey.Key, "must not persist") with
        {
            RevisionDate = checkpoint.AddDays(1)
        };
        context.CiphersApi.GetCipherAsync(context.AccountContext, response.Id, Arg.Any<CancellationToken>()).Returns(response);
        using var vault = context.OpenVault();
        context.CreateLease(vault, keySession);
        context.FailCheckpointWrites();

        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => context.VaultService.SyncCipherAsync(response.Id, CancellationToken.None));
        context.CloseUnitOfWork();

        Assert.Equal("original value", vault.GetCipher(response.Id)?.Name);
        using var persisted = context.OpenVaultFromFreshConnection(keySession);
        Assert.Equal("original value", persisted.GetCipher(response.Id)?.Name);
        Assert.Equal(checkpoint, context.GetServerRevisionDateFromFreshConnection());
        Assert.Empty(context.EventPublisher.ReceivedCalls());
    }

    [Fact]
    public async Task SyncFolder_updates_persisted_folder_and_publishes_event()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        DateTimeOffset checkpoint = VaultManagerTestData.RevisionDate.AddDays(-10);
        var existing = VaultManagerTestData.Folder("incremental-folder", "old folder", keySession.UserKey.Key);
        context.SeedCache(folders: [existing], serverRevisionDate: checkpoint);
        var response = VaultManagerTestData.Folder("incremental-folder", "new folder", keySession.UserKey.Key) with
        {
            RevisionDate = VaultManagerTestData.RevisionDate.AddDays(1)
        };
        context.FoldersApi.GetFolderAsync(context.AccountContext, response.Id, Arg.Any<CancellationToken>()).Returns(response);
        using var vault = context.OpenVault();
        context.CreateLease(vault, keySession);
        context.EventPublisher.When(p => p.PublishAsync(Arg.Any<FolderChangedEvent>(), Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                Assert.Equal("new folder", vault.GetFolder(response.Id)?.Name);
                using var committed = context.OpenVaultFromFreshConnection(keySession);
                Assert.Equal("new folder", committed.GetFolder(response.Id)?.Name);
            });

        await context.VaultService.SyncFolderAsync(response.Id, CancellationToken.None);

        Assert.Equal("new folder", vault.GetFolder(response.Id)?.Name);
        using var persisted = context.OpenVault();
        Assert.Equal("new folder", persisted.GetFolder(response.Id)?.Name);
        await context.EventPublisher.Received(1).PublishAsync(Arg.Is<FolderChangedEvent>(e =>
            e.UserId == context.Account.UserId && e.Folder.Id == response.Id &&
            e.Folder.RevisionDate == response.RevisionDate && e.Folder.Name == "new folder"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncFolder_advances_checkpoint_to_applied_revision()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var checkpoint = VaultManagerTestData.RevisionDate.AddDays(-10);
        context.SeedCache(serverRevisionDate: checkpoint);
        var response = VaultManagerTestData.Folder("checkpoint-folder", "folder", keySession.UserKey.Key);
        context.FoldersApi.GetFolderAsync(context.AccountContext, response.Id, Arg.Any<CancellationToken>()).Returns(response);
        using var vault = context.OpenVault();
        context.CreateLease(vault, keySession);

        await context.VaultService.SyncFolderAsync(response.Id, CancellationToken.None);

        Assert.Equal(response.RevisionDate, context.GetServerRevisionDate());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public async Task SyncFolder_obeys_revision_boundary(int offset)
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var id = FolderId.Parse("folder-boundary");
        var old = VaultManagerTestData.Folder("folder-boundary", "before", keySession.UserKey.Key) with { RevisionDate = VaultManagerTestData.RevisionDate };
        var response = VaultManagerTestData.Folder("folder-boundary", "after", keySession.UserKey.Key) with { RevisionDate = VaultManagerTestData.RevisionDate.AddDays(offset) };
        context.SeedCache(folders: [old]);
        context.FoldersApi.GetFolderAsync(context.AccountContext, id, Arg.Any<CancellationToken>()).Returns(response);
        using var vault = context.OpenVault();
        context.CreateLease(vault, keySession);

        await context.VaultService.SyncFolderAsync(id, CancellationToken.None);

        Assert.Equal(offset > 0 ? "after" : "before", vault.GetFolder(id)?.Name);
        Assert.Equal(offset > 0 ? response.RevisionDate : old.RevisionDate, vault.GetFolder(id)?.RevisionDate);
        Assert.Equal(offset > 0 ? response.RevisionDate : null, context.GetServerRevisionDate());
        using var persisted = context.OpenVault();
        Assert.Equal(offset > 0 ? "after" : "before", persisted.GetFolder(id)?.Name);
    }

    [Fact]
    public async Task SyncFolder_inserts_a_missing_folder()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var response = VaultManagerTestData.Folder("new-folder", "inserted", keySession.UserKey.Key);
        context.FoldersApi.GetFolderAsync(context.AccountContext, response.Id, Arg.Any<CancellationToken>()).Returns(response);
        using var vault = context.OpenVault();
        context.CreateLease(vault, keySession);

        using var cancellation = new CancellationTokenSource();
        await context.VaultService.SyncFolderAsync(response.Id, cancellation.Token);
        await context.FoldersApi.Received(1).GetFolderAsync(context.AccountContext, response.Id, cancellation.Token);

        Assert.Equal("inserted", vault.GetFolder(response.Id)?.Name);
        using var persisted = context.OpenVault();
        Assert.Equal("inserted", persisted.GetFolder(response.Id)?.Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SyncCipher_rejects_api_failure_or_cancellation_without_mutation(bool canceled)
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var id = CipherId.Parse("failure-cipher");
        var cached = VaultManagerTestData.Cipher("failure-cipher", keySession.UserKey.Key, "unchanged");
        var checkpoint = VaultManagerTestData.RevisionDate;
        context.SeedCache([cached], serverRevisionDate: checkpoint);
        using var vault = context.OpenVault();
        context.CreateLease(vault, keySession);
        context.CiphersApi.GetCipherAsync(context.AccountContext, id, Arg.Any<CancellationToken>())
            .Returns(_ => canceled ? Task.FromCanceled<CipherResponse>(new CancellationToken(true)) : Task.FromException<CipherResponse>(new HttpRequestException("offline")));

        if (canceled)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.VaultService.SyncCipherAsync(id, CancellationToken.None));
        }
        else
        {
            await Assert.ThrowsAsync<HttpRequestException>(() => context.VaultService.SyncCipherAsync(id, CancellationToken.None));
        }

        Assert.Equal("unchanged", vault.GetCipher(cached.Id)?.Name);
        using var persisted = context.OpenVault();
        Assert.Equal("unchanged", persisted.GetCipher(cached.Id)?.Name);
        Assert.Equal(checkpoint, context.GetServerRevisionDate());
        Assert.Empty(context.EventPublisher.ReceivedCalls());
    }

    [Fact]
    public async Task SyncCipher_rejects_invalid_encrypted_data_without_mutation()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var cached = VaultManagerTestData.Cipher("bad-cipher", keySession.UserKey.Key, "Unchanged");
        context.SeedCache([cached], serverRevisionDate: VaultManagerTestData.RevisionDate);
        var response = VaultManagerTestData.Cipher("bad-cipher", Enumerable.Repeat((byte)0x77, 64).ToArray()) with
        {
            RevisionDate = cached.RevisionDate.AddDays(1)
        };
        context.CiphersApi.GetCipherAsync(context.AccountContext, response.Id, Arg.Any<CancellationToken>()).Returns(response);
        using var vault = context.OpenVault();
        context.CreateLease(vault, keySession);

        await Assert.ThrowsAnyAsync<System.Security.Cryptography.CryptographicException>(() => context.VaultService.SyncCipherAsync(response.Id, CancellationToken.None));

        Assert.Equal("Unchanged", vault.GetCipher(response.Id)?.Name);
        using var persisted = context.OpenVaultFromFreshConnection(keySession);
        Assert.Equal("Unchanged", persisted.GetCipher(response.Id)?.Name);
        Assert.Equal(VaultManagerTestData.RevisionDate, context.GetServerRevisionDate());
        Assert.Empty(context.EventPublisher.ReceivedCalls());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SyncCipher_requires_unlocked_session(bool locked)
    {
        using var context = new VaultManagerTestContext();
        context.SessionAccessor.AppSessionState.Returns(locked
            ? new AppSessionState.Locked(context.Account)
            : new AppSessionState.NotAuthenticated());

        await Assert.ThrowsAsync<UnlockedSessionRequiredException>(() => context.VaultService.SyncCipherAsync(CipherId.Parse("locked"), CancellationToken.None));

        Assert.Empty(context.CiphersApi.ReceivedCalls());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SyncFolder_requires_unlocked_session(bool locked)
    {
        using var context = new VaultManagerTestContext();
        context.SessionAccessor.AppSessionState.Returns(locked
            ? new AppSessionState.Locked(context.Account)
            : new AppSessionState.NotAuthenticated());

        await Assert.ThrowsAsync<UnlockedSessionRequiredException>(() => context.VaultService.SyncFolderAsync(FolderId.Parse("locked-folder"), CancellationToken.None));

        Assert.Empty(context.FoldersApi.ReceivedCalls());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SyncFolder_rejects_api_failure_or_cancellation_without_mutation(bool canceled)
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var id = FolderId.Parse("failure-folder");
        var cached = VaultManagerTestData.Folder("failure-folder", "unchanged", keySession.UserKey.Key);
        var checkpoint = VaultManagerTestData.RevisionDate;
        context.SeedCache(folders: [cached], serverRevisionDate: checkpoint);
        using var vault = context.OpenVault();
        context.CreateLease(vault, keySession);
        context.FoldersApi.GetFolderAsync(context.AccountContext, id, Arg.Any<CancellationToken>())
            .Returns(_ => canceled ? Task.FromCanceled<FolderResponse>(new CancellationToken(true)) : Task.FromException<FolderResponse>(new HttpRequestException("offline")));

        if (canceled)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.VaultService.SyncFolderAsync(id, CancellationToken.None));
        }
        else
        {
            await Assert.ThrowsAsync<HttpRequestException>(() => context.VaultService.SyncFolderAsync(id, CancellationToken.None));
        }

        Assert.Equal("unchanged", vault.GetFolder(id)?.Name);
        using var persisted = context.OpenVault();
        Assert.Equal("unchanged", persisted.GetFolder(id)?.Name);
        Assert.Equal(checkpoint, context.GetServerRevisionDate());
        Assert.Empty(context.EventPublisher.ReceivedCalls());
    }

    [Fact]
    public async Task SyncFolder_rejects_invalid_encrypted_data_without_mutation()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var cached = VaultManagerTestData.Folder("bad-folder", "Unchanged", keySession.UserKey.Key);
        context.SeedCache(folders: [cached], serverRevisionDate: VaultManagerTestData.RevisionDate);
        var response = VaultManagerTestData.Folder("bad-folder", "bad", Enumerable.Repeat((byte)0x77, 64).ToArray()) with
        {
            RevisionDate = cached.RevisionDate.AddDays(1)
        };
        context.FoldersApi.GetFolderAsync(context.AccountContext, response.Id, Arg.Any<CancellationToken>()).Returns(response);
        using var vault = context.OpenVault();
        context.CreateLease(vault, keySession);

        await Assert.ThrowsAnyAsync<System.Security.Cryptography.CryptographicException>(() => context.VaultService.SyncFolderAsync(response.Id, CancellationToken.None));

        Assert.Equal("Unchanged", vault.GetFolder(response.Id)?.Name);
        using var persisted = context.OpenVaultFromFreshConnection(keySession);
        Assert.Equal("Unchanged", persisted.GetFolder(response.Id)?.Name);
        Assert.Equal(VaultManagerTestData.RevisionDate, context.GetServerRevisionDate());
        Assert.Empty(context.EventPublisher.ReceivedCalls());
    }
}
