using System.Globalization;
using FluentBitwarden.AppHost.Infrastructure.Data.Mapping;

namespace FluentBitwarden.AppHost.Modules.Vault.Persistence.Mapping;

internal static class VaultFolderMapper
{
    public readonly record struct FolderRow(
        string FolderId,
        long RevisionDateUnixMs,
        byte[] EncryptedName);

    public static VaultFolderResponse ToDomain(in FolderRow row) => new()
    {
        Id = FolderId.Parse(row.FolderId, CultureInfo.InvariantCulture),
        RevisionDate = row.RevisionDateUnixMs.ToDateTimeOffsetFromUnixMs(),
        EncryptedName = EncString.FromBytes(row.EncryptedName)
    };

    public readonly record struct FolderInsertParameters(
        string UserId,
        string FolderId,
        long RevisionDateUnixMs,
        byte[] EncryptedName);

    public static FolderInsertParameters ToInsertParameters(string userId, in VaultFolderResponse dto) => new(
        UserId: userId,
        FolderId: dto.Id.ToString(),
        RevisionDateUnixMs: dto.RevisionDate.ToUnixMs(),
        EncryptedName: dto.EncryptedName.ToByteArray());
}
