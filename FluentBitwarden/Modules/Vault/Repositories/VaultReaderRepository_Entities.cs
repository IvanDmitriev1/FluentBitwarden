using BitwardenApi.Models;

namespace FluentBitwarden.Modules.Vault.Repositories;

internal partial class VaultReaderRepository
{
    private readonly record struct FolderRow(
        string FolderId,
        long RevisionDateUnixMs,
        string? EncryptedName);

    private static VaultFolderDto ToDto(in FolderRow row) => new()
    {
        Id = FolderId.Parse(row.FolderId),
        RevisionDate = DateTimeOffset.FromUnixTimeMilliseconds(row.RevisionDateUnixMs),
        EncryptedName = row.EncryptedName
    };

    private readonly record struct CollectionRow(
        string CollectionId,
        string? OrganizationId,
        int ReadOnly,
        int Manage,
        int HidePasswords,
        int? CollectionType,
        string? EncryptedName);

    private static VaultCollectionDto ToDto(in CollectionRow row) => new()
    {
        Id = CollectionId.Parse(row.CollectionId),
        OrganizationId = row.OrganizationId is null ? null : OrganizationId.Parse(row.OrganizationId),
        ReadOnly = row.ReadOnly != 0,
        Manage = row.Manage != 0,
        HidePasswords = row.HidePasswords != 0,
        Type = row.CollectionType,
        EncryptedName = row.EncryptedName
    };

    private readonly record struct CipherRow(
        int RowId,
        string CipherId,
        string? OrganizationId,
        string? FolderId,
        string? EncryptedKey,
        int CipherType,
        long RevisionDateUnixMs,
        long CreationDateUnixMs,
        long? DeletedDateUnixMs,
        long? ArchivedDateUnixMs,
        int Favorite,
        int Reprompt,
        int Edit,
        int ViewPassword);

    private static VaultCipherDto ToDto(in CipherRow row) => new()
    {
        Id = CipherId.Parse(row.CipherId),
        OrganizationId = row.OrganizationId is null
            ? null
            : OrganizationId.Parse(row.OrganizationId),
        FolderId = row.FolderId is null
            ? null
            : FolderId.Parse(row.FolderId),
        EncryptedKey = row.EncryptedKey,
        CipherType = (CipherType)row.CipherType,
        RevisionDate = DateTimeOffset.FromUnixTimeMilliseconds(row.RevisionDateUnixMs),
        CreationDate = DateTimeOffset.FromUnixTimeMilliseconds(row.CreationDateUnixMs),
        DeletedDate = row.DeletedDateUnixMs is { } deletedDateUnixMs
            ? DateTimeOffset.FromUnixTimeMilliseconds(deletedDateUnixMs)
            : null,
        ArchivedDate = row.ArchivedDateUnixMs is { } archivedDateUnixMs
            ? DateTimeOffset.FromUnixTimeMilliseconds(archivedDateUnixMs)
            : null,
        Favorite = row.Favorite != 0,
        Reprompt = row.Reprompt != 0,
        Edit = row.Edit != 0,
        ViewPassword = row.ViewPassword != 0,
        Data = []
    };
}
