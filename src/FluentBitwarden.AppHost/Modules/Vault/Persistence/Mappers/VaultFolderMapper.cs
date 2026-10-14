using System.Globalization;
using BitwardenApi.Infrastructure.Cryptography.Enc;
using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.AppHost.Infrastructure.Data;

namespace FluentBitwarden.AppHost.Modules.Vault.Persistence.Mappers;

internal class VaultFolderMapper
{
    public record FolderRow(
        string FolderId,
        long RevisionDateUnixMs,
        byte[] EncryptedName);

    public static VaultFolderResponse ToDomain(in FolderRow row) => new()
    {
        Id = FolderId.Parse(row.FolderId, CultureInfo.InvariantCulture),
        RevisionDate = row.RevisionDateUnixMs.ToDateTimeOffsetFromUnixMs(),
        EncryptedName = EncString.FromBytes(row.EncryptedName)
    };

    public record FolderInsertParameters(
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
