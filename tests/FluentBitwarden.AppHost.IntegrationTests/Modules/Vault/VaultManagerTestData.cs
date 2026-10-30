using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using static FluentBitwarden.AppHost.IntegrationTests.Modules.Vault.VaultParserTestData;

namespace FluentBitwarden.AppHost.IntegrationTests.Modules.Vault;

internal static class VaultManagerTestData
{
    public static readonly DateTimeOffset RevisionDate = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    public static CipherResponse Cipher(
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
        var cipher = CreateDto(CipherType.Login, payload);
        return cipher with { Id = CipherId.Parse(id), FolderId = FolderId.Empty };
    }

    public static FolderResponse Folder(string id, string name, ReadOnlySpan<byte> userKey) => new()
    {
        Id = FolderId.Parse(id),
        RevisionDate = RevisionDate,
        EncryptedName = EncString.Encrypt(name, userKey)
    };

    public static VaultSyncResponse SyncResponse(
        UserId userId,
        CipherResponse[] ciphers,
        FolderResponse[] folders) => new()
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
