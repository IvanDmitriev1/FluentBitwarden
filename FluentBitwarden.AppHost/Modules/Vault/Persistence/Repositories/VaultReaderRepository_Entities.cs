using BitwardenApi.Vault.Attachments.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.KeyResolution;

namespace FluentBitwarden.AppHost.Modules.Vault.Persistence.Repositories;

internal partial class VaultReaderRepository
{
    internal readonly record struct FolderRow(
        string FolderId,
        long RevisionDateUnixMs,
        byte[] EncryptedName);

    private static VaultFolderResponse ToDto(in FolderRow row) => new()
    {
        Id = FolderId.Parse(row.FolderId),
        RevisionDate = DateTimeOffset.FromUnixTimeMilliseconds(row.RevisionDateUnixMs),
        EncryptedName = EncString.FromBytes(row.EncryptedName)
    };

    internal readonly record struct OrganizationRow(
        string UserId,
        string OrganizationId,
        string? OrganizationUserId,
        string OrganizationName,
        int IsEnabled,
        int AccessSecretsManager,
        int? MemberStatus,
        byte[]? ProtectedOrganizationKey);

    private static VaultOrganizationResponse ToDto(in OrganizationRow row) => new()
    {
        Id = OrganizationId.Parse(row.OrganizationId),
        UserId = UserId.Parse(row.UserId),
        OrganizationUserId = row.OrganizationUserId is null
            ? Guid.Empty
            : Guid.Parse(row.OrganizationUserId),
        Name = row.OrganizationName,
        Enabled = row.IsEnabled != 0,
        AccessSecretsManager = row.AccessSecretsManager != 0,
        Status = row.MemberStatus ?? -1,
        ProtectedOrganizationKey = row.ProtectedOrganizationKey is null
            ? AsymmetricEncString.Empty
            : AsymmetricEncString.FromBytes(row.ProtectedOrganizationKey)
    };

    internal readonly record struct CollectionRow(
        string CollectionId,
        string? OrganizationId,
        int IsReadOnly,
        int CanManage,
        int HidePasswords,
        int CollectionType,
        byte[] EncryptedName);

    private static VaultCollectionResponse ToDto(in CollectionRow row) => new()
    {
        Id = CollectionId.Parse(row.CollectionId),
        OrganizationId = row.OrganizationId is null ? OrganizationId.Empty : OrganizationId.Parse(row.OrganizationId),
        ReadOnly = row.IsReadOnly != 0,
        Manage = row.CanManage != 0,
        HidePasswords = row.HidePasswords != 0,
        Type = row.CollectionType,
        EncryptedName = EncString.FromBytes(row.EncryptedName)
    };

    internal readonly record struct CipherRow(
        int RowId,
        string CipherId,
        string? OrganizationId,
        string? FolderId,
        byte[]? ProtectedCipherKey,
        int CipherType,
        long RevisionDateUnixMs,
        long CreationDateUnixMs,
        long? DeletedDateUnixMs,
        long? ArchivedDateUnixMs,
        int IsFavorite,
        int Reprompt,
        int CanEdit,
        int CanViewPassword);

    internal readonly record struct CipherCollectionRow(
        string CipherId,
        string CollectionId);

    internal readonly record struct CipherAttachmentRow(
        string CipherId,
        string AttachmentId,
        byte[] EncryptedFileName,
        long Size);

    internal sealed class CipherKeyMaterialRow
    {
        public required string CipherId { get; init; }
        public string? OrganizationId { get; init; }
        public byte[]? ProtectedCipherKey { get; init; }
    }

    private static VaultCipherResponse ToDto(
        in CipherRow row,
        CollectionId[] collectionIds,
        VaultCipherAttachmentDownloadResponse[] attachments) => new()
    {
        Id = CipherId.Parse(row.CipherId),
        OrganizationId = row.OrganizationId is null
            ? OrganizationId.Empty
            : OrganizationId.Parse(row.OrganizationId),
        FolderId = row.FolderId is null
            ? FolderId.Empty
            : FolderId.Parse(row.FolderId),
        CollectionIds = collectionIds,
        ProtectedCipherKey = row.ProtectedCipherKey is null ? EncString.Empty : EncString.FromBytes(row.ProtectedCipherKey),
        VaultCipherType = (VaultCipherType)row.CipherType,
        RevisionDate = DateTimeOffset.FromUnixTimeMilliseconds(row.RevisionDateUnixMs),
        CreationDate = DateTimeOffset.FromUnixTimeMilliseconds(row.CreationDateUnixMs),
        DeletedDate = row.DeletedDateUnixMs is { } deletedDateUnixMs
            ? DateTimeOffset.FromUnixTimeMilliseconds(deletedDateUnixMs)
            : null,
        ArchivedDate = row.ArchivedDateUnixMs is { } archivedDateUnixMs
            ? DateTimeOffset.FromUnixTimeMilliseconds(archivedDateUnixMs)
            : null,
        Favorite = row.IsFavorite != 0,
        Reprompt = row.Reprompt != 0,
        Edit = row.CanEdit != 0,
        ViewPassword = row.CanViewPassword != 0,
        Data = [],
        Attachments = attachments
    };

    private static VaultCipherAttachmentDownloadResponse ToDto(in CipherAttachmentRow row) => new()
    {
        Id = AttachmentId.Parse(row.AttachmentId),
        Url = string.Empty,
        EncryptedFileName = EncString.FromBytes(row.EncryptedFileName),
        ProtectedAttachmentKey = EncString.Empty,
        Size = FileSize.FromBytes(row.Size)
    };

    private static VaultCipherKeyMaterial ToKeyMaterial(CipherKeyMaterialRow row) => new(
        CipherId: CipherId.Parse(row.CipherId),
        OrganizationId: row.OrganizationId is null
            ? OrganizationId.Empty
            : OrganizationId.Parse(row.OrganizationId),
        ProtectedCipherKey: row.ProtectedCipherKey is null
            ? EncString.Empty
            : EncString.FromBytes(row.ProtectedCipherKey));
}
