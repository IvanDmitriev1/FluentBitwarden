using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Modules.Vault.Persistence;

namespace FluentBitwarden.AppHost.IntegrationTests.Modules.Vault;

public sealed class VaultWriterRepositoryTests
{
    [Fact]
    public void UpdateFolder_inserts_and_updates_without_losing_assignments_or_changing_other_accounts()
    {
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "vault@example.test", "first");
        var otherAccount = AccountTestData.Profile(AccountTestData.SecondUserId, "other@example.test", "second");
        using var database = new AccountRepositoryTestDatabase();
        AccountRepositoryTestHelper.InsertAccount(database, account);
        AccountRepositoryTestHelper.InsertAccount(database, otherAccount);
        byte[] key = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        var original = VaultManagerTestData.Folder("folder-a", "Original", key);
        var other = VaultManagerTestData.Folder("folder-a", "Other account", key);
        var updated = VaultManagerTestData.Folder("folder-a", "Updated", key);
        updated.RevisionDate = original.RevisionDate.AddMinutes(1);
        var added = VaultManagerTestData.Folder("folder-b", "Added", key);
        var cipher = VaultManagerTestData.Cipher("cipher-a", key) with { FolderId = original.Id };

        using (var unitOfWork = database.CreateUnitOfWork())
        {
            var writer = new VaultWriterRepository(unitOfWork);
            unitOfWork.Begin();
            writer.WriteFolders(account.UserId, [original]);
            writer.WriteFolders(otherAccount.UserId, [other]);
            writer.WriteCiphers(account.UserId, [cipher]);
            unitOfWork.Commit();

            unitOfWork.Begin();
            writer.UpdateFolder(account.UserId, in updated);
            writer.UpdateFolder(account.UserId, in added);
            unitOfWork.Commit();
        }

        using var reopened = database.CreateUnitOfWork();
        var reader = new VaultReaderRepository(reopened);
        var folders = reader.GetAllFolders(account.UserId);
        Assert.Equal(2, folders.Length);
        var restored = Assert.Single(folders, folder => folder.Id == updated.Id);
        Assert.Equal(updated.EncryptedName, restored.EncryptedName);
        Assert.Equal(updated.RevisionDate, restored.RevisionDate);
        Assert.Equal(added.EncryptedName, Assert.Single(folders, folder => folder.Id == added.Id).EncryptedName);
        Assert.Equal(other.EncryptedName, Assert.Single(reader.GetAllFolders(otherAccount.UserId)).EncryptedName);
        var restoredCipher = Assert.Single(VaultRepositoryTestData.ReadCiphers(reader, account.UserId));
        Assert.Equal(original.Id, restoredCipher.Response.FolderId);
    }
}
