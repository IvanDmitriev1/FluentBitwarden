using System.Globalization;
using BitwardenApi.Vault.Attachments.Contracts;
using FluentBitwarden.AppHost.Infrastructure.Data.Mapping;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Models;

namespace FluentBitwarden.AppHost.Modules.Vault.Persistence.Mapping;

internal static class VaultCipherMapper
{
    public readonly record struct CipherRow(
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

    public readonly record struct CipherCollectionRow(
        string CipherId,
        string CollectionId);

    public readonly record struct CipherAttachmentRow(
        string CipherId,
        string AttachmentId,
        byte[] EncryptedFileName,
        long Size);

    public sealed class CipherKeyMaterialRow
    {
        public required string CipherId { get; init; }
        public string? OrganizationId { get; init; }
        public byte[]? ProtectedCipherKey { get; init; }
    }

    public static VaultCipherResponse ToDomain(
        in CipherRow row,
        CollectionId[] collectionIds,
        VaultCipherAttachmentDownloadResponse[] attachments) => new()
    {
        Id = CipherId.Parse(row.CipherId, CultureInfo.InvariantCulture),
        OrganizationId = SqliteConversions.ParseOrEmpty(row.OrganizationId, OrganizationId.Parse, OrganizationId.Empty),
        FolderId = SqliteConversions.ParseOrEmpty(row.FolderId, FolderId.Parse, FolderId.Empty),
        CollectionIds = collectionIds,
        ProtectedCipherKey = row.ProtectedCipherKey is null ? EncString.Empty : EncString.FromBytes(row.ProtectedCipherKey),
        VaultCipherType = (VaultCipherType)row.CipherType,
        RevisionDate = row.RevisionDateUnixMs.ToDateTimeOffsetFromUnixMs(),
        CreationDate = row.CreationDateUnixMs.ToDateTimeOffsetFromUnixMs(),
        DeletedDate = row.DeletedDateUnixMs.ToDateTimeOffsetFromUnixMs(),
        ArchivedDate = row.ArchivedDateUnixMs.ToDateTimeOffsetFromUnixMs(),
        Favorite = row.IsFavorite.ToBool(),
        Reprompt = row.Reprompt.ToBool(),
        Edit = row.CanEdit.ToBool(),
        ViewPassword = row.CanViewPassword.ToBool(),
        Data = [],
        Attachments = attachments
    };

    public static VaultCipherAttachmentDownloadResponse ToDomain(in CipherAttachmentRow row) => new()
    {
        Id = AttachmentId.Parse(row.AttachmentId, CultureInfo.InvariantCulture),
        Url = string.Empty,
        EncryptedFileName = EncString.FromBytes(row.EncryptedFileName),
        ProtectedAttachmentKey = EncString.Empty,
        Size = FileSize.FromBytes(row.Size)
    };

    public static VaultCipherKeyMaterial ToKeyMaterial(CipherKeyMaterialRow row) => new(
        CipherId: CipherId.Parse(row.CipherId, CultureInfo.InvariantCulture),
        OrganizationId: SqliteConversions.ParseOrEmpty(row.OrganizationId, OrganizationId.Parse, OrganizationId.Empty),
        ProtectedCipherKey: row.ProtectedCipherKey is null
            ? EncString.Empty
            : EncString.FromBytes(row.ProtectedCipherKey));

    public readonly record struct CipherInsertParameters(
        string UserId,
        string CipherId,
        string? OrganizationId,
        int CipherType,
        long RevisionDateUnixMs,
        long CreationDateUnixMs,
        long? DeletedDateUnixMs,
        long? ArchivedDateUnixMs,
        int Favorite,
        int Reprompt,
        int Edit,
        int ViewPassword,
        byte[]? ProtectedCipherKey,
        int Size);

    public static CipherInsertParameters ToInsertParameters(string userId, string cipherId, in VaultCipherResponse dto) => new(
        UserId: userId,
        CipherId: cipherId,
        OrganizationId: dto.OrganizationId.IsEmpty
            ? null
            : dto.OrganizationId.ToString(),
        CipherType: (int)dto.VaultCipherType,
        RevisionDateUnixMs: dto.RevisionDate.ToUnixMs(),
        CreationDateUnixMs: dto.CreationDate.ToUnixMs(),
        DeletedDateUnixMs: dto.DeletedDate.ToUnixMs(),
        ArchivedDateUnixMs: dto.ArchivedDate.ToUnixMs(),
        Favorite: dto.Favorite.ToSqliteInt(),
        Reprompt: dto.Reprompt.ToSqliteInt(),
        Edit: dto.Edit.ToSqliteInt(),
        ViewPassword: dto.ViewPassword.ToSqliteInt(),
        ProtectedCipherKey: dto.ProtectedCipherKey.IsEmpty
            ? null
            : dto.ProtectedCipherKey.ToByteArray(),
        Size: dto.Data.Length);

    public readonly record struct CipherAttachmentInsertParameters(
        string UserId,
        string CipherId,
        string AttachmentId,
        int SortOrder,
        byte[] EncryptedFileName,
        long Size);

    public static CipherAttachmentInsertParameters ToAttachmentInsertParameters(
        string userId,
        string cipherId,
        int sortOrder,
        in VaultCipherAttachmentDownloadResponse attachment) => new(
        UserId: userId,
        CipherId: cipherId,
        AttachmentId: attachment.Id.ToString(),
        SortOrder: sortOrder,
        EncryptedFileName: attachment.EncryptedFileName.ToByteArray(),
        Size: attachment.Size.Bytes);
}
