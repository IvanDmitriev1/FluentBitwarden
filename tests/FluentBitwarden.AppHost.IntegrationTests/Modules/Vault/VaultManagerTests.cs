using System.Text.Json;

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

        using var vault = context.VaultFactory.Open(context.AccountContext, keySession);

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
        var service = new VaultFactory(null!);

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
        var result = await context.VaultSyncHandler.SyncAsync();

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
        var result = await context.VaultSyncHandler.SyncAsync();

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
        var result = await context.VaultSyncHandler.SyncAsync();

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
        var result = await context.VaultSyncHandler.SyncAsync();

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
        var result = await context.VaultSyncHandler.SyncAsync();

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
        var result = await context.VaultSyncHandler.SyncAsync();

        Assert.Equal(VaultSyncResult.Failed, result);
        Assert.Equal(lastRevision, context.GetServerRevisionDate());
        Assert.Equal("Synthetic item", vault.GetCipher(cachedCipher.Id)?.Name);
        Assert.Equal("Cached folder", Assert.Single(vault.GetFolders()).Name);
        using var cachedVault = context.OpenVault();
        Assert.Equal("Synthetic item", cachedVault.GetCipher(cachedCipher.Id)?.Name);
        Assert.Equal("Cached folder", Assert.Single(cachedVault.GetFolders()).Name);
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
        var saved = await context.VaultCipherService.SaveAsync(cipher, CancellationToken.None);

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
        context.CiphersApi.GetCipherAsync(
                context.AccountContext,
                existingCipher.Id,
                Arg.Any<CancellationToken>())
            .Returns(existingCipher);
        context.CiphersApi.UpdateCipherAsync(
                Arg.Any<BitwardenAccountContext>(),
                existingCipher.Id,
                Arg.Any<CipherRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(updatedResponse);
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = existingCipher.Id;
        cipher.RevisionDate = existingCipher.RevisionDate;
        cipher.Name = "Edited input";
        using var vault = context.OpenVault();

        context.CreateLease(vault, keySession);
        var saved = await context.VaultCipherService.SaveAsync(cipher, CancellationToken.None);

        Assert.Equal(existingCipher.Id, saved.Id);
        Assert.Equal("Updated item", saved.Name);
        await context.CiphersApi.Received(1).UpdateCipherAsync(
            context.AccountContext,
            existingCipher.Id,
            Arg.Any<CipherRequest>(),
            Arg.Any<CancellationToken>());
        await context.CiphersApi.Received(1).GetCipherAsync(
            context.AccountContext,
            existingCipher.Id,
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
    public async Task SaveCipher_preserves_rich_server_data_and_reopens_the_updated_item_from_sqlite()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        byte[] cipherKey = [.. Enumerable.Range(65, 64).Select(static value => (byte)value)];
        byte[] attachmentKey = [.. Enumerable.Range(129, 64).Select(static value => (byte)value)];
        EncString protectedCipherKey = EncString.Encrypt(cipherKey, keySession.UserKey.Key);
        byte[] data = VaultParserTestData.WritePayload(cipherKey.ToArray(), static (writer, key) =>
        {
            writer.WriteStartObject();
            VaultParserTestData.WriteEncrypted(writer, "name", "Original item", key);
            VaultParserTestData.WriteEncrypted(writer, "notes", "Original notes", key);
            VaultParserTestData.WriteEncrypted(writer, "username", "original-user", key);
            VaultParserTestData.WriteEncrypted(writer, "password", "original-password", key);
            VaultParserTestData.WriteEncrypted(writer, "totp", "unsupported secret format", key);
            writer.WritePropertyName("fields");
            writer.WriteStartArray();
            writer.WriteStartObject();
            VaultParserTestData.WriteEncrypted(writer, "name", "Linked field", key);
            VaultParserTestData.WriteEncrypted(writer, "value", "Linked field value", key);
            writer.WriteNumber("type", 1);
            writer.WriteNumber("linkedId", 100);
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WritePropertyName("passwordHistory");
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WriteString("lastUsedDate", "2025-04-03T02:01:00Z");
            VaultParserTestData.WriteEncrypted(writer, "password", "historical password", key);
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WritePropertyName("uris");
            writer.WriteStartArray();
            VaultParserTestData.WriteLoginUri(writer, key, "https://example.test");
            writer.WriteEndArray();
            writer.WritePropertyName("fido2Credentials");
            writer.WriteStartArray();
            VaultParserTestData.WriteFido2Credential(writer, key, "12345678-1234-5678-9abc-def012345678");
            VaultParserTestData.WriteFido2Credential(writer, key, "12345678-1234-5678-9abc-def012345679");
            writer.WriteEndArray();
            writer.WriteStartObject("futureLoginProperty");
            writer.WriteString("opaque", "preserved encrypted payload metadata");
            writer.WriteEndObject();
            writer.WriteEndObject();
        });
        AttachmentDownloadResponse attachment = new()
        {
            Id = AttachmentId.Parse("existing-attachment"),
            Url = "https://example.test/attachment",
            EncryptedFileName = EncString.Encrypt("old-file.txt", cipherKey),
            ProtectedAttachmentKey = EncString.Encrypt(attachmentKey, cipherKey),
            Size = FileSize.FromBytes(12)
        };
        CipherResponse baseline = VaultManagerTestData.Cipher("existing-cipher", keySession.UserKey.Key) with
        {
            ProtectedCipherKey = protectedCipherKey,
            Data = data,
            ArchivedDate = new DateTimeOffset(2026, 9, 29, 10, 0, 0, TimeSpan.Zero),
            Attachments = [attachment]
        };
        DateTimeOffset savedRevision = baseline.RevisionDate.AddMinutes(1);
        context.SeedCache([baseline]);
        context.CiphersApi.GetCipherAsync(context.AccountContext, baseline.Id, Arg.Any<CancellationToken>()).Returns(baseline);
        CipherRequest? capturedRequest = null;
        context.CiphersApi.UpdateCipherAsync(
                context.AccountContext,
                baseline.Id,
                Arg.Any<CipherRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedRequest = callInfo.Arg<CipherRequest>();
                return Task.FromResult(baseline with
                {
                    ProtectedCipherKey = capturedRequest.Key,
                    Data = capturedRequest.Data!,
                    ArchivedDate = capturedRequest.ArchivedDate is { } archivedDate
                        ? new DateTimeOffset(archivedDate, TimeSpan.Zero)
                        : null,
                    RevisionDate = savedRevision
                });
            });

        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = baseline.Id;
        cipher.RevisionDate = baseline.RevisionDate;
        cipher.Name = "Updated item";
        cipher.Notes = "Updated notes";
        cipher.Username = "updated-user";
        cipher.Password = "updated-password";
        cipher.Uris.Add(new LoginUri { Value = "https://example.test", Match = LoginUri.MatchType.Domain });
        using var vault = context.OpenVault();
        context.CreateLease(vault, keySession);

        LoginCipher saved = Assert.IsType<LoginCipher>(await context.VaultCipherService.SaveAsync(cipher, CancellationToken.None));

        Assert.Equal("Updated item", saved.Name);
        Assert.Equal("updated-user", saved.Username);
        Assert.Equal("old-file.txt", Assert.Single(saved.Attachments).FileName);
        Assert.Equal(savedRevision, saved.RevisionDate);
        Assert.NotNull(capturedRequest);
        Assert.Equal(protectedCipherKey, capturedRequest.Key);
        Assert.Equal(baseline.ArchivedDate?.UtcDateTime, capturedRequest.ArchivedDate);
        using (JsonDocument updatedData = JsonDocument.Parse(capturedRequest.Data!))
        {
            JsonElement root = updatedData.RootElement;
            Assert.Equal(JsonValueKind.String, root.GetProperty("username").ValueKind);
            Assert.Equal(1, root.GetProperty("fields").GetArrayLength());
            Assert.Equal(100, root.GetProperty("fields")[0].GetProperty("linkedId").GetInt32());
            Assert.Equal("2025-04-03T02:01:00Z", root.GetProperty("passwordHistory")[0].GetProperty("lastUsedDate").GetString());
            Assert.Equal(2, root.GetProperty("fido2Credentials").GetArrayLength());
            Assert.True(root.GetProperty("uris")[0].TryGetProperty("unknownMetadata", out _));
            Assert.Equal("preserved encrypted payload metadata", root.GetProperty("futureLoginProperty").GetProperty("opaque").GetString());
            using JsonDocument originalData = JsonDocument.Parse(baseline.Data);
            Assert.Equal(
                originalData.RootElement.GetProperty("totp").GetRawText(),
                root.GetProperty("totp").GetRawText());
        }

        using var reopened = context.OpenVaultFromFreshConnection(keySession);
        LoginCipher persisted = Assert.IsType<LoginCipher>(reopened.GetCipher(baseline.Id));
        Assert.Equal("Updated item", persisted.Name);
        Assert.Equal("updated-user", persisted.Username);
        Assert.Equal("old-file.txt", Assert.Single(persisted.Attachments).FileName);
        var persistedKey = context.VaultReaderRepository.GetCipherKeyMaterial(context.AccountContext.UserId, baseline.Id);
        Assert.Equal(protectedCipherKey, persistedKey.ProtectedCipherKey);
    }

    [Theory]
    [InlineData("id")]
    [InlineData("type")]
    [InlineData("revision")]
    public async Task SaveCipher_rejects_a_mismatched_baseline_before_mutating_local_or_remote_state(string mismatch)
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var cachedCipher = VaultManagerTestData.Cipher("existing-cipher", keySession.UserKey.Key);
        context.SeedCache([cachedCipher]);
        CipherResponse serverCipher = mismatch switch
        {
            "id" => cachedCipher with { Id = CipherId.Parse("different-cipher") },
            "type" => cachedCipher with { VaultCipherType = CipherType.Card },
            "revision" => cachedCipher with { RevisionDate = cachedCipher.RevisionDate.AddSeconds(1) },
            _ => throw new ArgumentOutOfRangeException(nameof(mismatch))
        };
        context.CiphersApi.GetCipherAsync(context.AccountContext, cachedCipher.Id, Arg.Any<CancellationToken>()).Returns(serverCipher);
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = cachedCipher.Id;
        cipher.RevisionDate = cachedCipher.RevisionDate;
        using var vault = context.OpenVault();
        context.CreateLease(vault, keySession);

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.VaultCipherService.SaveAsync(cipher, CancellationToken.None));

        _ = context.CiphersApi.DidNotReceive().UpdateCipherAsync(
            Arg.Any<BitwardenAccountContext>(), Arg.Any<CipherId>(), Arg.Any<CipherRequest>(), Arg.Any<CancellationToken>());
        Assert.Equal("Synthetic item", vault.GetCipher(cachedCipher.Id)?.Name);
        using var reopened = context.OpenVault();
        Assert.Equal("Synthetic item", reopened.GetCipher(cachedCipher.Id)?.Name);
    }

    [Fact]
    public async Task SaveCipher_rejects_organization_ciphers_before_mutating_them()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var cachedCipher = VaultManagerTestData.Cipher("existing-cipher", keySession.UserKey.Key);
        CipherResponse organizationCipher = cachedCipher with { OrganizationId = OrganizationId.Parse("22222222-2222-2222-2222-222222222222") };
        context.SeedCache([cachedCipher]);
        context.CiphersApi.GetCipherAsync(context.AccountContext, cachedCipher.Id, Arg.Any<CancellationToken>()).Returns(organizationCipher);
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = cachedCipher.Id;
        cipher.RevisionDate = cachedCipher.RevisionDate;
        using var vault = context.OpenVault();
        context.CreateLease(vault, keySession);

        await Assert.ThrowsAsync<NotSupportedException>(() => context.VaultCipherService.SaveAsync(cipher, CancellationToken.None));

        _ = context.CiphersApi.DidNotReceive().UpdateCipherAsync(
            Arg.Any<BitwardenAccountContext>(), Arg.Any<CipherId>(), Arg.Any<CipherRequest>(), Arg.Any<CancellationToken>());
        Assert.Equal("Synthetic item", vault.GetCipher(cachedCipher.Id)?.Name);
    }

    [Fact]
    public async Task SaveCipher_rejects_duplicate_uri_entries_with_ambiguous_metadata_before_put()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        byte[] data = VaultParserTestData.WritePayload(keySession.UserKey.Key.ToArray(), static (writer, key) =>
        {
            writer.WriteStartObject();
            VaultParserTestData.WriteEncrypted(writer, "name", "Synthetic item", key);
            VaultParserTestData.WriteEncrypted(writer, "username", "synthetic-user", key);
            writer.WritePropertyName("uris");
            writer.WriteStartArray();
            WriteUriWithChecksum(writer, key, "first-checksum");
            WriteUriWithChecksum(writer, key, "second-checksum");
            writer.WriteEndArray();
            writer.WriteEndObject();
        });
        var cachedCipher = VaultManagerTestData.Cipher("existing-cipher", keySession.UserKey.Key) with { Data = data };
        context.SeedCache([cachedCipher]);
        context.CiphersApi.GetCipherAsync(context.AccountContext, cachedCipher.Id, Arg.Any<CancellationToken>()).Returns(cachedCipher);
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = cachedCipher.Id;
        cipher.RevisionDate = cachedCipher.RevisionDate;
        cipher.Uris.Add(new LoginUri { Value = "https://example.test", Match = LoginUri.MatchType.Domain });
        cipher.Uris.Add(new LoginUri { Value = "https://example.test", Match = LoginUri.MatchType.Domain });
        using var vault = context.OpenVault();
        context.CreateLease(vault, keySession);

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.VaultCipherService.SaveAsync(cipher, CancellationToken.None));

        _ = context.CiphersApi.DidNotReceive().UpdateCipherAsync(
            Arg.Any<BitwardenAccountContext>(), Arg.Any<CipherId>(), Arg.Any<CipherRequest>(), Arg.Any<CancellationToken>());
        Assert.Equal("Synthetic item", vault.GetCipher(cachedCipher.Id)?.Name);
        using var reopened = context.OpenVault();
        Assert.Equal("Synthetic item", reopened.GetCipher(cachedCipher.Id)?.Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveCipher_does_not_update_or_persist_when_baseline_fetch_fails_or_is_canceled(bool cancelFetch)
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var cachedCipher = VaultManagerTestData.Cipher("existing-cipher", keySession.UserKey.Key);
        context.SeedCache([cachedCipher]);
        using var cancellationSource = new CancellationTokenSource();
        if (cancelFetch)
            cancellationSource.Cancel();
        context.CiphersApi.GetCipherAsync(context.AccountContext, cachedCipher.Id, cancellationSource.Token)
            .Returns(cancelFetch
                ? Task.FromCanceled<CipherResponse>(cancellationSource.Token)
                : Task.FromException<CipherResponse>(new InvalidOperationException("Fetch failed.")));
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = cachedCipher.Id;
        cipher.RevisionDate = cachedCipher.RevisionDate;
        using var vault = context.OpenVault();
        context.CreateLease(vault, keySession);

        if (cancelFetch)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                context.VaultCipherService.SaveAsync(cipher, cancellationSource.Token));
        }
        else
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                context.VaultCipherService.SaveAsync(cipher, cancellationSource.Token));
        }

        _ = context.CiphersApi.DidNotReceive().UpdateCipherAsync(
            Arg.Any<BitwardenAccountContext>(), Arg.Any<CipherId>(), Arg.Any<CipherRequest>(), Arg.Any<CancellationToken>());
        Assert.Equal("Synthetic item", vault.GetCipher(cachedCipher.Id)?.Name);
        using var reopened = context.OpenVault();
        Assert.Equal("Synthetic item", reopened.GetCipher(cachedCipher.Id)?.Name);
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
        context.CiphersApi.GetCipherAsync(
                context.AccountContext,
                existingCipher.Id,
                Arg.Any<CancellationToken>())
            .Returns(existingCipher);
        context.CiphersApi.UpdateCipherAsync(
                Arg.Any<BitwardenAccountContext>(),
                existingCipher.Id,
                Arg.Any<CipherRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CipherResponse>(new InvalidOperationException("Save failed.")));
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = existingCipher.Id;
        cipher.RevisionDate = existingCipher.RevisionDate;
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
        context.CiphersApi.GetCipherAsync(
                context.AccountContext,
                existingCipher.Id,
                Arg.Any<CancellationToken>())
            .Returns(existingCipher);
        context.CiphersApi.UpdateCipherAsync(
                Arg.Any<BitwardenAccountContext>(),
                existingCipher.Id,
                Arg.Any<CipherRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(malformedCipher);
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = existingCipher.Id;
        cipher.RevisionDate = existingCipher.RevisionDate;
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
        return context.VaultCipherService.SaveAsync(cipher, cancellationToken);
    }

    private static void WriteUriWithChecksum(Utf8JsonWriter writer, ReadOnlySpan<byte> key, string checksum)
    {
        writer.WriteStartObject();
        VaultParserTestData.WriteEncrypted(writer, "uri", "https://example.test", key);
        writer.WriteNumber("match", (int)LoginUri.MatchType.Domain);
        writer.WriteString("checksum", checksum);
        writer.WriteEndObject();
    }
}


