using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Modules.Vault.Persistence;

namespace FluentBitwarden.AppHost.IntegrationTests.Modules.Vault;

public sealed class VaultReaderRepositoryTests
{
    [Fact]
    public void Read_reopens_organizations_collections_cipher_membership_and_ordered_attachments_from_sqlite()
    {
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "vault@example.test", "first");
        var otherAccount = AccountTestData.Profile(AccountTestData.SecondUserId, "other@example.test", "second");
        using var database = new AccountRepositoryTestDatabase();
        AccountRepositoryTestHelper.InsertAccount(database, account);
        AccountRepositoryTestHelper.InsertAccount(database, otherAccount);

        OrganizationId organizationId = OrganizationId.Parse("22222222-2222-2222-2222-222222222222");
        CollectionId collectionAId = CollectionId.Parse("collection-a");
        CollectionId collectionBId = CollectionId.Parse("collection-b");
        FolderId folderId = FolderId.Parse("folder-a");
        CipherId sharedCipherId = CipherId.Parse("shared-cipher");
        byte[] userKey = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        byte[] organizationKey = Enumerable.Range(65, 64).Select(static value => (byte)value).ToArray();
        byte[] cipherKey = Enumerable.Range(129, 64).Select(static value => (byte)value).ToArray();
        byte[] otherUserKey = Enumerable.Repeat((byte)193, 64).ToArray();
        byte[] otherOrganizationKey = Enumerable.Repeat((byte)194, 64).ToArray();
        byte[] otherCipherKey = Enumerable.Repeat((byte)195, 64).ToArray();
        using var accountKeys = new AccountKeySessionTestContext(userKey);
        AsymmetricEncString protectedOrganizationKey = accountKeys.EncryptOrganizationKey(organizationKey);
        EncString encryptedCipherKey = EncString.Encrypt(cipherKey, organizationKey);
        AsymmetricEncString otherProtectedOrganizationKey = accountKeys.EncryptOrganizationKey(otherOrganizationKey);
        EncString otherEncryptedCipherKey = EncString.Encrypt(otherCipherKey, otherOrganizationKey);
        var organization = new VaultOrganizationResponse
        {
            Id = organizationId,
            UserId = account.UserId,
            OrganizationUserId = Guid.Parse("12345678-1234-5678-9abc-def012345678"),
            Name = "Synthetic organization",
            Status = 2,
            Enabled = true,
            AccessSecretsManager = true,
            ProtectedOrganizationKey = protectedOrganizationKey
        };
        var collectionB = new CollectionResponse
        {
            Id = collectionBId,
            OrganizationId = organizationId,
            ReadOnly = true,
            Manage = false,
            HidePasswords = true,
            Type = 2,
            EncryptedName = EncString.Encrypt("Collection B", organizationKey)
        };
        var collectionA = new CollectionResponse
        {
            Id = collectionAId,
            OrganizationId = organizationId,
            ReadOnly = false,
            Manage = true,
            HidePasswords = false,
            Type = 1,
            EncryptedName = EncString.Encrypt("Collection A", organizationKey)
        };
        var folder = VaultManagerTestData.Folder("folder-a", "Synthetic folder", userKey);
        var otherOrganization = new VaultOrganizationResponse
        {
            Id = organizationId,
            UserId = otherAccount.UserId,
            OrganizationUserId = Guid.Parse("22345678-1234-5678-9abc-def012345678"),
            Name = "Other synthetic organization",
            Status = 1,
            Enabled = true,
            ProtectedOrganizationKey = otherProtectedOrganizationKey
        };
        var otherCollectionA = new CollectionResponse
        {
            Id = collectionAId,
            OrganizationId = organizationId,
            EncryptedName = EncString.Encrypt("Other collection A", otherOrganizationKey)
        };
        var otherCollectionB = new CollectionResponse
        {
            Id = collectionBId,
            OrganizationId = organizationId,
            ReadOnly = false,
            Manage = true,
            HidePasswords = false,
            Type = 3,
            EncryptedName = EncString.Encrypt("Other collection B", otherOrganizationKey)
        };
        var otherFolder = VaultManagerTestData.Folder("folder-a", "Other user folder", otherUserKey);
        var attachmentB = VaultParserTestData.CreateAttachment("attachment-b", "second.txt", 8, cipherKey);
        var attachmentA = VaultParserTestData.CreateAttachment("attachment-a", "first.txt", 5, cipherKey);
        var otherAttachmentA = VaultParserTestData.CreateAttachment("attachment-a", "other-first.txt", 11, otherCipherKey);
        var otherAttachmentB = VaultParserTestData.CreateAttachment("attachment-b", "other-second.txt", 13, otherCipherKey);
        CipherResponse cipher = VaultManagerTestData.Cipher("shared-cipher", cipherKey) with
        {
            OrganizationId = organizationId,
            FolderId = folderId,
            ProtectedCipherKey = encryptedCipherKey,
            CollectionIds = [collectionBId, collectionAId],
            Attachments = [attachmentB, attachmentA]
        };
        CipherResponse otherUserCipher = VaultManagerTestData.Cipher("shared-cipher", otherCipherKey, "Other user cipher") with
        {
            OrganizationId = organizationId,
            FolderId = folderId,
            ProtectedCipherKey = otherEncryptedCipherKey,
            CollectionIds = [collectionAId],
            Attachments = [otherAttachmentA, otherAttachmentB]
        };

        Assert.NotEqual(protectedOrganizationKey, otherProtectedOrganizationKey);
        Assert.NotEqual(collectionA.EncryptedName, otherCollectionA.EncryptedName);
        Assert.NotEqual(folder.EncryptedName, otherFolder.EncryptedName);
        Assert.NotEqual(cipher.Data, otherUserCipher.Data);
        Assert.NotEqual(attachmentA.EncryptedFileName, otherAttachmentA.EncryptedFileName);

        using (UnitOfWork unitOfWork = database.CreateUnitOfWork())
        {
            var writer = new VaultWriterRepository(unitOfWork);
            unitOfWork.Begin();
            writer.WriteOrganizations(account.UserId, [organization]);
            writer.WriteOrganizations(otherAccount.UserId, [otherOrganization]);
            writer.WriteFolders(account.UserId, [folder]);
            writer.WriteFolders(otherAccount.UserId, [otherFolder]);
            writer.WriteCollections(account.UserId, [collectionB, collectionA]);
            writer.WriteCollections(otherAccount.UserId, [otherCollectionA, otherCollectionB]);
            writer.WriteCiphers(account.UserId, [cipher]);
            writer.WriteCiphers(otherAccount.UserId, [otherUserCipher]);
            unitOfWork.Commit();
        }

        using var reopenedUnitOfWork = database.CreateUnitOfWork();
        var reader = new VaultReaderRepository(reopenedUnitOfWork);

        var organizationKeys = reader.LoadOrganizationKeys(account.UserId);
        Assert.Equal(new[] { organizationId }, organizationKeys.Keys);
        Assert.Equal(protectedOrganizationKey, organizationKeys[organizationId]);
        var otherOrganizationKeys = reader.LoadOrganizationKeys(otherAccount.UserId);
        Assert.Equal(new[] { organizationId }, otherOrganizationKeys.Keys);
        Assert.Equal(otherProtectedOrganizationKey, otherOrganizationKeys[organizationId]);
        CollectionResponse[] collections = reader.GetAllCollections(account.UserId);
        Assert.Equal(new[] { collectionBId, collectionAId }, collections.Select(static item => item.Id));
        Assert.Equal(collectionB.EncryptedName, collections[0].EncryptedName);
        Assert.Equal(collectionA.EncryptedName, collections[1].EncryptedName);
        Assert.Equal(2, collections[0].Type);
        Assert.True(collections[0].ReadOnly);
        Assert.False(collections[0].Manage);
        Assert.True(collections[0].HidePasswords);
        Assert.False(collections[1].ReadOnly);
        Assert.True(collections[1].Manage);
        Assert.False(collections[1].HidePasswords);
        Assert.Equal(1, collections[1].Type);
        CollectionResponse[] otherCollections = reader.GetAllCollections(otherAccount.UserId);
        Assert.Equal(new[] { collectionAId, collectionBId }, otherCollections.Select(static item => item.Id));
        Assert.Equal(new[] { otherCollectionA.EncryptedName, otherCollectionB.EncryptedName },
            otherCollections.Select(static item => item.EncryptedName));
        Assert.Equal(new[] { 0, 3 }, otherCollections.Select(static item => item.Type));
        FolderResponse reopenedFolder = Assert.Single(reader.GetAllFolders(account.UserId));
        Assert.Equal(folder.Id, reopenedFolder.Id);
        Assert.Equal(folder.RevisionDate, reopenedFolder.RevisionDate);
        Assert.Equal(folder.EncryptedName, reopenedFolder.EncryptedName);
        FolderResponse reopenedOtherFolder = Assert.Single(reader.GetAllFolders(otherAccount.UserId));
        Assert.Equal(otherFolder.Id, reopenedOtherFolder.Id);
        Assert.Equal(otherFolder.EncryptedName, reopenedOtherFolder.EncryptedName);

        (CipherResponse Response, byte[] Payload) reopenedCipher = Assert.Single(
            VaultRepositoryTestData.ReadCiphers(reader, account.UserId));
        Assert.Equal(sharedCipherId, reopenedCipher.Response.Id);
        Assert.Equal(organizationId, reopenedCipher.Response.OrganizationId);
        Assert.Equal(folderId, reopenedCipher.Response.FolderId);
        Assert.Equal(encryptedCipherKey, reopenedCipher.Response.ProtectedCipherKey);
        Assert.Equal(cipher.Data, reopenedCipher.Payload);
        Assert.Equal(
            new[] { collectionAId, collectionBId },
            reopenedCipher.Response.CollectionIds);
        AttachmentDownloadResponse[] reopenedAttachments = reopenedCipher.Response.Attachments
            ?? throw new InvalidOperationException("The persisted cipher lost its attachments.");
        Assert.Equal(new[] { "attachment-b", "attachment-a" }, reopenedAttachments.Select(static item => item.Id.ToString()));
        Assert.Equal(
            new[] { attachmentB.EncryptedFileName, attachmentA.EncryptedFileName },
            reopenedAttachments.Select(static item => item.EncryptedFileName));
        Assert.Equal(new[] { 8L, 5L }, reopenedAttachments.Select(static item => item.Size.Bytes));

        (CipherResponse Response, byte[] Payload) reopenedOtherCipher = Assert.Single(
            VaultRepositoryTestData.ReadCiphers(reader, otherAccount.UserId));
        Assert.Equal(sharedCipherId, reopenedOtherCipher.Response.Id);
        Assert.Equal(organizationId, reopenedOtherCipher.Response.OrganizationId);
        Assert.Equal(folderId, reopenedOtherCipher.Response.FolderId);
        Assert.Equal(otherEncryptedCipherKey, reopenedOtherCipher.Response.ProtectedCipherKey);
        Assert.NotEqual(reopenedCipher.Payload, reopenedOtherCipher.Payload);
        Assert.Equal(new[] { collectionAId }, reopenedOtherCipher.Response.CollectionIds);
        AttachmentDownloadResponse[] reopenedOtherAttachments = reopenedOtherCipher.Response.Attachments
            ?? throw new InvalidOperationException("The other user's persisted cipher lost its attachments.");
        Assert.Equal(new[] { "attachment-a", "attachment-b" },
            reopenedOtherAttachments.Select(static item => item.Id.ToString()));
        Assert.Equal(new[] { otherAttachmentA.EncryptedFileName, otherAttachmentB.EncryptedFileName },
            reopenedOtherAttachments.Select(static item => item.EncryptedFileName));
        Assert.Equal(new[] { 11L, 13L }, reopenedOtherAttachments.Select(static item => item.Size.Bytes));

        var ownerCipherKey = reader.GetCipherKeyMaterial(account.UserId, sharedCipherId);
        Assert.Equal(sharedCipherId, ownerCipherKey.CipherId);
        Assert.Equal(organizationId, ownerCipherKey.OrganizationId);
        Assert.Equal(encryptedCipherKey, ownerCipherKey.ProtectedCipherKey);
        var otherCipherKeyMaterial = reader.GetCipherKeyMaterial(otherAccount.UserId, sharedCipherId);
        Assert.Equal(sharedCipherId, otherCipherKeyMaterial.CipherId);
        Assert.Equal(organizationId, otherCipherKeyMaterial.OrganizationId);
        Assert.Equal(otherEncryptedCipherKey, otherCipherKeyMaterial.ProtectedCipherKey);
    }

    [Fact]
    public void Empty_vault_read_returns_no_organizations_folders_collections_or_ciphers()
    {
        AccountProfile account = AccountTestData.Profile(AccountTestData.FirstUserId, "vault@example.test", "first");
        using var database = new AccountRepositoryTestDatabase();
        AccountRepositoryTestHelper.InsertAccount(database, account);
        using var unitOfWork = database.CreateUnitOfWork();
        var reader = new VaultReaderRepository(unitOfWork);

        Assert.Empty(reader.LoadOrganizationKeys(account.UserId));
        Assert.Empty(reader.GetAllFolders(account.UserId));
        Assert.Empty(reader.GetAllCollections(account.UserId));
        Assert.Empty(VaultRepositoryTestData.ReadCiphers(reader, account.UserId));
    }

    [Fact]
    public void Upserting_cipher_replaces_its_memberships_and_attachments_without_touching_other_rows()
    {
        AccountProfile account = AccountTestData.Profile(AccountTestData.FirstUserId, "vault@example.test", "first");
        AccountProfile otherAccount = AccountTestData.Profile(AccountTestData.SecondUserId, "other@example.test", "second");
        using var database = new AccountRepositoryTestDatabase();
        AccountRepositoryTestHelper.InsertAccount(database, account);
        AccountRepositoryTestHelper.InsertAccount(database, otherAccount);

        OrganizationId organizationId = OrganizationId.Parse("22222222-2222-2222-2222-222222222222");
        CollectionId oldCollectionId = CollectionId.Parse("old-collection");
        CollectionId newCollectionId = CollectionId.Parse("new-collection");
        FolderId oldFolderId = FolderId.Parse("old-folder");
        FolderId newFolderId = FolderId.Parse("new-folder");
        CipherId cipherId = CipherId.Parse("shared-cipher");
        byte[] userKey = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        byte[] organizationKey = Enumerable.Range(65, 64).Select(static value => (byte)value).ToArray();
        byte[] otherUserKey = Enumerable.Range(129, 64).Select(static value => (byte)value).ToArray();
        byte[] otherOrganizationKey = Enumerable.Repeat((byte)201, 64).ToArray();
        byte[] otherCipherKey = Enumerable.Repeat((byte)202, 64).ToArray();
        byte[] firstCipherKey = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        byte[] updatedCipherKey = Enumerable.Range(65, 64).Select(static value => (byte)value).ToArray();
        using var accountKeys = new AccountKeySessionTestContext(userKey);
        AsymmetricEncString protectedOrganizationKey = accountKeys.EncryptOrganizationKey(organizationKey);
        AsymmetricEncString otherProtectedOrganizationKey = accountKeys.EncryptOrganizationKey(otherOrganizationKey);
        var organization = new VaultOrganizationResponse
        {
            Id = organizationId,
            UserId = account.UserId,
            OrganizationUserId = Guid.Parse("12345678-1234-5678-9abc-def012345678"),
            Name = "Synthetic organization",
            Status = 2,
            Enabled = true,
            AccessSecretsManager = true,
            ProtectedOrganizationKey = protectedOrganizationKey
        };
        var oldCollection = new CollectionResponse
        {
            Id = oldCollectionId,
            OrganizationId = organizationId,
            EncryptedName = EncString.Encrypt("Old collection", organizationKey)
        };
        var newCollection = new CollectionResponse
        {
            Id = newCollectionId,
            OrganizationId = organizationId,
            EncryptedName = EncString.Encrypt("New collection", organizationKey)
        };
        var otherOrganization = new VaultOrganizationResponse
        {
            Id = organizationId,
            UserId = otherAccount.UserId,
            OrganizationUserId = Guid.Parse("22345678-1234-5678-9abc-def012345678"),
            Name = "Other organization",
            Status = 1,
            Enabled = true,
            ProtectedOrganizationKey = otherProtectedOrganizationKey
        };
        var otherOldCollection = new CollectionResponse
        {
            Id = oldCollectionId,
            OrganizationId = organizationId,
            EncryptedName = EncString.Encrypt("Other old collection", otherOrganizationKey)
        };
        var otherNewCollection = new CollectionResponse
        {
            Id = newCollectionId,
            OrganizationId = organizationId,
            EncryptedName = EncString.Encrypt("Other new collection", otherOrganizationKey)
        };
        var oldFolder = VaultManagerTestData.Folder("old-folder", "Old folder", userKey);
        var newFolder = VaultManagerTestData.Folder("new-folder", "New folder", userKey);
        var otherOldFolder = VaultManagerTestData.Folder("old-folder", "Other old folder", otherUserKey);
        var otherNewFolder = VaultManagerTestData.Folder("new-folder", "Other new folder", otherUserKey);
        CipherResponse original = VaultManagerTestData.Cipher("shared-cipher", firstCipherKey, "Original") with
        {
            OrganizationId = organizationId,
            FolderId = oldFolderId,
            ProtectedCipherKey = EncString.Encrypt(firstCipherKey, organizationKey),
            CollectionIds = [oldCollectionId],
            Attachments = [VaultParserTestData.CreateAttachment("old-attachment", "old.txt", 3, firstCipherKey)]
        };
        CipherResponse otherUserCipher = VaultManagerTestData.Cipher("shared-cipher", otherCipherKey, "Other user") with
        {
            OrganizationId = organizationId,
            FolderId = oldFolderId,
            ProtectedCipherKey = EncString.Encrypt(otherCipherKey, otherOrganizationKey),
            CollectionIds = [oldCollectionId, newCollectionId],
            Attachments = [
                VaultParserTestData.CreateAttachment("old-attachment", "other-old.txt", 17, otherCipherKey),
                VaultParserTestData.CreateAttachment("new-attachment-a", "other-new-a.txt", 19, otherCipherKey)
            ]
        };
        CipherResponse updated = VaultManagerTestData.Cipher("shared-cipher", updatedCipherKey, "Updated") with
        {
            OrganizationId = organizationId,
            FolderId = newFolderId,
            ProtectedCipherKey = EncString.Encrypt(updatedCipherKey, organizationKey),
            CollectionIds = [newCollectionId],
            Attachments = [
                VaultParserTestData.CreateAttachment("new-attachment-b", "new-b.txt", 9, updatedCipherKey),
                VaultParserTestData.CreateAttachment("new-attachment-a", "new-a.txt", 7, updatedCipherKey)
            ]
        };
        CipherResponse unrelatedCipher = VaultManagerTestData.Cipher("unrelated-cipher", userKey);

        using (UnitOfWork unitOfWork = database.CreateUnitOfWork())
        {
            var writer = new VaultWriterRepository(unitOfWork);
            unitOfWork.Begin();
            writer.WriteOrganizations(account.UserId, [organization]);
            writer.WriteOrganizations(otherAccount.UserId, [otherOrganization]);
            writer.WriteFolders(account.UserId, [oldFolder, newFolder]);
            writer.WriteFolders(otherAccount.UserId, [otherOldFolder, otherNewFolder]);
            writer.WriteCollections(account.UserId, [oldCollection, newCollection]);
            writer.WriteCollections(otherAccount.UserId, [otherOldCollection, otherNewCollection]);
            writer.WriteCiphers(account.UserId, [original, unrelatedCipher]);
            writer.WriteCiphers(otherAccount.UserId, [otherUserCipher]);
            unitOfWork.Commit();
        }

        using (UnitOfWork unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            new VaultWriterRepository(unitOfWork).UpsertCipher(account.UserId, in updated);
            unitOfWork.Commit();
        }

        using var reopenedUnitOfWork = database.CreateUnitOfWork();
        var reader = new VaultReaderRepository(reopenedUnitOfWork);
        List<(CipherResponse Response, byte[] Payload)> ownerCiphers = VaultRepositoryTestData.ReadCiphers(reader, account.UserId);
        Assert.Equal(2, ownerCiphers.Count);
        (CipherResponse Response, byte[] Payload) reopenedUpdated = Assert.Single(
            ownerCiphers,
            item => item.Response.Id == cipherId);
        Assert.Equal(updated.Data, reopenedUpdated.Payload);
        Assert.Equal(newFolderId, reopenedUpdated.Response.FolderId);
        Assert.Equal(updated.ProtectedCipherKey, reopenedUpdated.Response.ProtectedCipherKey);
        Assert.Equal(new[] { newCollectionId }, reopenedUpdated.Response.CollectionIds);
        AttachmentDownloadResponse[] updatedAttachments = reopenedUpdated.Response.Attachments
            ?? throw new InvalidOperationException("The updated cipher lost its attachments.");
        Assert.Equal(
            new[] { "new-attachment-b", "new-attachment-a" },
            updatedAttachments.Select(static item => item.Id.ToString()));
        (CipherResponse Response, byte[] Payload) retainedCipher = Assert.Single(
            ownerCiphers,
            item => item.Response.Id != cipherId);
        Assert.Equal(unrelatedCipher.Id, retainedCipher.Response.Id);
        Assert.Equal(unrelatedCipher.Data, retainedCipher.Payload);

        var ownerOrganizationKeys = reader.LoadOrganizationKeys(account.UserId);
        Assert.Equal(new[] { organizationId }, ownerOrganizationKeys.Keys);
        Assert.Equal(protectedOrganizationKey, ownerOrganizationKeys[organizationId]);
        var otherOrganizationKeys = reader.LoadOrganizationKeys(otherAccount.UserId);
        Assert.Equal(new[] { organizationId }, otherOrganizationKeys.Keys);
        Assert.Equal(otherProtectedOrganizationKey, otherOrganizationKeys[organizationId]);
        FolderResponse[] ownerFolders = reader.GetAllFolders(account.UserId);
        Assert.Equal(new[] { oldFolderId, newFolderId }, ownerFolders.Select(static item => item.Id));
        Assert.Equal(new[] { oldFolder.EncryptedName, newFolder.EncryptedName },
            ownerFolders.Select(static item => item.EncryptedName));
        FolderResponse[] otherFolders = reader.GetAllFolders(otherAccount.UserId);
        Assert.Equal(new[] { oldFolderId, newFolderId }, otherFolders.Select(static item => item.Id));
        Assert.Equal(new[] { otherOldFolder.EncryptedName, otherNewFolder.EncryptedName },
            otherFolders.Select(static item => item.EncryptedName));
        CollectionResponse[] ownerCollections = reader.GetAllCollections(account.UserId);
        Assert.Equal(new[] { oldCollectionId, newCollectionId }, ownerCollections.Select(static item => item.Id));
        Assert.Equal(new[] { oldCollection.EncryptedName, newCollection.EncryptedName },
            ownerCollections.Select(static item => item.EncryptedName));
        CollectionResponse[] otherCollections = reader.GetAllCollections(otherAccount.UserId);
        Assert.Equal(new[] { oldCollectionId, newCollectionId }, otherCollections.Select(static item => item.Id));
        Assert.Equal(new[] { otherOldCollection.EncryptedName, otherNewCollection.EncryptedName },
            otherCollections.Select(static item => item.EncryptedName));
        Assert.NotEqual(oldCollection.EncryptedName, otherOldCollection.EncryptedName);
        Assert.NotEqual(oldFolder.EncryptedName, otherOldFolder.EncryptedName);

        (CipherResponse Response, byte[] Payload) otherUserReopened = Assert.Single(
            VaultRepositoryTestData.ReadCiphers(reader, otherAccount.UserId));
        Assert.Equal(cipherId, otherUserReopened.Response.Id);
        Assert.Equal(organizationId, otherUserReopened.Response.OrganizationId);
        Assert.Equal(oldFolderId, otherUserReopened.Response.FolderId);
        Assert.Equal(
            new[] { oldCollectionId, newCollectionId }.OrderBy(static id => id.ToString(), StringComparer.Ordinal),
            otherUserReopened.Response.CollectionIds.OrderBy(static id => id.ToString(), StringComparer.Ordinal));
        Assert.Equal(otherUserCipher.Data, otherUserReopened.Payload);
        Assert.NotEqual(updated.Data, otherUserReopened.Payload);
        Assert.Equal(otherUserCipher.ProtectedCipherKey, otherUserReopened.Response.ProtectedCipherKey);
        AttachmentDownloadResponse[] otherAttachments = otherUserReopened.Response.Attachments
            ?? throw new InvalidOperationException("The other user's cipher lost its attachments after the update.");
        Assert.Equal(new[] { "old-attachment", "new-attachment-a" },
            otherAttachments.Select(static item => item.Id.ToString()));
        Assert.Equal(
            otherUserCipher.Attachments!.Select(static item => item.EncryptedFileName),
            otherAttachments.Select(static item => item.EncryptedFileName));
        Assert.Equal(new[] { 17L, 19L }, otherAttachments.Select(static item => item.Size.Bytes));
        Assert.NotEqual(updatedAttachments[1].EncryptedFileName, otherAttachments[1].EncryptedFileName);
    }

    [Fact]
    public void Replacing_owner_snapshot_with_empty_state_leaves_another_users_vault_intact()
    {
        AccountProfile account = AccountTestData.Profile(AccountTestData.FirstUserId, "vault@example.test", "first");
        AccountProfile otherAccount = AccountTestData.Profile(AccountTestData.SecondUserId, "other@example.test", "second");
        using var database = new AccountRepositoryTestDatabase();
        AccountRepositoryTestHelper.InsertAccount(database, account);
        AccountRepositoryTestHelper.InsertAccount(database, otherAccount);
        byte[] ownerKey = Enumerable.Repeat((byte)1, 64).ToArray();
        OrganizationId ownerOrganizationId = OrganizationId.Parse("33333333-3333-3333-3333-333333333333");
        CollectionId ownerCollectionId = CollectionId.Parse("owner-collection");
        byte[] ownerOrganizationKey = Enumerable.Repeat((byte)3, 64).ToArray();
        byte[] otherUserKey = Enumerable.Repeat((byte)4, 64).ToArray();
        byte[] otherOrganizationKey = Enumerable.Repeat((byte)5, 64).ToArray();
        byte[] otherCipherKey = Enumerable.Repeat((byte)6, 64).ToArray();
        using var accountKeys = new AccountKeySessionTestContext(ownerKey);
        AsymmetricEncString protectedOwnerOrganizationKey = accountKeys.EncryptOrganizationKey(ownerOrganizationKey);
        AsymmetricEncString protectedOtherOrganizationKey = accountKeys.EncryptOrganizationKey(otherOrganizationKey);
        var ownerOrganization = new VaultOrganizationResponse
        {
            Id = ownerOrganizationId,
            UserId = account.UserId,
            OrganizationUserId = Guid.Parse("12345678-1234-5678-9abc-def012345678"),
            Name = "Owner organization",
            Status = 2,
            Enabled = true,
            ProtectedOrganizationKey = protectedOwnerOrganizationKey
        };
        var ownerCollection = new CollectionResponse
        {
            Id = ownerCollectionId,
            OrganizationId = ownerOrganizationId,
            EncryptedName = EncString.Encrypt("Owner collection", ownerOrganizationKey)
        };
        var otherOrganization = new VaultOrganizationResponse
        {
            Id = ownerOrganizationId,
            UserId = otherAccount.UserId,
            OrganizationUserId = Guid.Parse("22345678-1234-5678-9abc-def012345678"),
            Name = "Other organization",
            Status = 1,
            Enabled = true,
            ProtectedOrganizationKey = protectedOtherOrganizationKey
        };
        var otherCollection = new CollectionResponse
        {
            Id = ownerCollectionId,
            OrganizationId = ownerOrganizationId,
            EncryptedName = EncString.Encrypt("Other collection", otherOrganizationKey)
        };
        var ownerFolder = VaultManagerTestData.Folder("owner-folder", "Owner folder", ownerKey);
        var otherFolder = VaultManagerTestData.Folder("owner-folder", "Other folder", otherUserKey);
        CipherResponse ownerCipher = VaultManagerTestData.Cipher("owner-cipher", ownerKey) with
        {
            OrganizationId = ownerOrganizationId,
            FolderId = ownerFolder.Id,
            CollectionIds = [ownerCollectionId],
            Attachments = [VaultParserTestData.CreateAttachment("owner-attachment", "owner.txt", 4, ownerKey)]
        };
        CipherResponse otherCipher = VaultManagerTestData.Cipher("owner-cipher", otherCipherKey, "Other owner cipher") with
        {
            OrganizationId = ownerOrganizationId,
            FolderId = ownerFolder.Id,
            ProtectedCipherKey = EncString.Encrypt(otherCipherKey, otherOrganizationKey),
            CollectionIds = [ownerCollectionId],
            Attachments = [VaultParserTestData.CreateAttachment("owner-attachment", "other-owner.txt", 12, otherCipherKey)]
        };

        Assert.NotEqual(protectedOwnerOrganizationKey, protectedOtherOrganizationKey);
        Assert.NotEqual(ownerCollection.EncryptedName, otherCollection.EncryptedName);
        Assert.NotEqual(ownerFolder.EncryptedName, otherFolder.EncryptedName);
        Assert.NotEqual(ownerCipher.Data, otherCipher.Data);

        using (UnitOfWork unitOfWork = database.CreateUnitOfWork())
        {
            var writer = new VaultWriterRepository(unitOfWork);
            unitOfWork.Begin();
            writer.WriteOrganizations(account.UserId, [ownerOrganization]);
            writer.WriteOrganizations(otherAccount.UserId, [otherOrganization]);
            writer.WriteFolders(account.UserId, [ownerFolder]);
            writer.WriteFolders(otherAccount.UserId, [otherFolder]);
            writer.WriteCollections(account.UserId, [ownerCollection]);
            writer.WriteCollections(otherAccount.UserId, [otherCollection]);
            writer.WriteCiphers(account.UserId, [ownerCipher]);
            writer.WriteCiphers(otherAccount.UserId, [otherCipher]);
            unitOfWork.Commit();
        }

        using (UnitOfWork unitOfWork = database.CreateUnitOfWork())
        {
            var writer = new VaultWriterRepository(unitOfWork);
            unitOfWork.Begin();
            writer.WriteOrganizations(account.UserId, []);
            writer.WriteFolders(account.UserId, []);
            writer.WriteCollections(account.UserId, []);
            writer.WriteCiphers(account.UserId, []);
            unitOfWork.Commit();
        }

        using var reopenedUnitOfWork = database.CreateUnitOfWork();
        var reader = new VaultReaderRepository(reopenedUnitOfWork);
        Assert.Empty(reader.LoadOrganizationKeys(account.UserId));
        Assert.Empty(reader.GetAllFolders(account.UserId));
        Assert.Empty(reader.GetAllCollections(account.UserId));
        Assert.Empty(VaultRepositoryTestData.ReadCiphers(reader, account.UserId));
        var otherOrganizationKeys = reader.LoadOrganizationKeys(otherAccount.UserId);
        Assert.Equal(new[] { ownerOrganizationId }, otherOrganizationKeys.Keys);
        Assert.Equal(protectedOtherOrganizationKey, otherOrganizationKeys[ownerOrganizationId]);
        FolderResponse reopenedOtherFolder = Assert.Single(reader.GetAllFolders(otherAccount.UserId));
        Assert.Equal(otherFolder.Id, reopenedOtherFolder.Id);
        Assert.Equal(otherFolder.EncryptedName, reopenedOtherFolder.EncryptedName);
        CollectionResponse reopenedOtherCollection = Assert.Single(reader.GetAllCollections(otherAccount.UserId));
        Assert.Equal(ownerCollectionId, reopenedOtherCollection.Id);
        Assert.Equal(otherCollection.EncryptedName, reopenedOtherCollection.EncryptedName);
        (CipherResponse Response, byte[] Payload) reopenedOtherCipher = Assert.Single(
            VaultRepositoryTestData.ReadCiphers(reader, otherAccount.UserId));
        Assert.Equal(ownerCipher.Id, reopenedOtherCipher.Response.Id);
        Assert.Equal(otherCipher.Data, reopenedOtherCipher.Payload);
        Assert.Equal(ownerOrganizationId, reopenedOtherCipher.Response.OrganizationId);
        Assert.Equal(ownerFolder.Id, reopenedOtherCipher.Response.FolderId);
        Assert.Equal(new[] { ownerCollectionId }, reopenedOtherCipher.Response.CollectionIds);
        AttachmentDownloadResponse[] otherAttachments = reopenedOtherCipher.Response.Attachments
            ?? throw new InvalidOperationException("The other user's cipher lost its attachment after snapshot removal.");
        Assert.Equal(new[] { "owner-attachment" }, otherAttachments.Select(static item => item.Id.ToString()));
        Assert.Equal(otherCipher.Attachments![0].EncryptedFileName, otherAttachments[0].EncryptedFileName);
        Assert.Equal(12L, otherAttachments[0].Size.Bytes);
        Assert.NotEqual(ownerCipher.Attachments![0].EncryptedFileName, otherAttachments[0].EncryptedFileName);
    }

    [Fact]
    public void Uncommitted_snapshot_replacement_rolls_back_to_the_persisted_cipher()
    {
        AccountProfile account = AccountTestData.Profile(AccountTestData.FirstUserId, "vault@example.test", "first");
        using var database = new AccountRepositoryTestDatabase();
        AccountRepositoryTestHelper.InsertAccount(database, account);
        byte[] key = Enumerable.Repeat((byte)7, 64).ToArray();
        CipherResponse original = VaultManagerTestData.Cipher("persisted-cipher", key, "Persisted");

        using (UnitOfWork unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            new VaultWriterRepository(unitOfWork).WriteCiphers(account.UserId, [original]);
            unitOfWork.Commit();
        }

        using (UnitOfWork unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            new VaultWriterRepository(unitOfWork).WriteCiphers(
                account.UserId,
                [VaultManagerTestData.Cipher("uncommitted-cipher", key, "Uncommitted")]);
        }

        using var reopenedUnitOfWork = database.CreateUnitOfWork();
        var reopened = Assert.Single(VaultRepositoryTestData.ReadCiphers(
            new VaultReaderRepository(reopenedUnitOfWork), account.UserId));
        Assert.Equal(original.Id, reopened.Response.Id);
        Assert.Equal(original.Data, reopened.Payload);
    }
}
