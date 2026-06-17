namespace FluentBitwarden.Modules.Vault.Repositories;

internal partial class VaultReaderRepository
{
    internal readonly record struct FolderRow(
        string FolderId,
        long RevisionDateUnixMs,
        byte[] EncryptedName);

    private static VaultFolderDto ToDto(in FolderRow row) => new()
    {
        Id = FolderId.Parse(row.FolderId),
        RevisionDate = DateTimeOffset.FromUnixTimeMilliseconds(row.RevisionDateUnixMs),
        EncryptedName = EncString.FromBytes(row.EncryptedName)
    };

    internal readonly record struct OrganizationRow(
        string OrganizationId,
        string? OrganizationUserId,
        string OrganizationName,
        int IsEnabled,
        int UseKeyConnector,
        int? MemberStatus,
        int? MemberType,
        byte[]? EncryptedOrganizationKey);

    private static VaultOrganizationDto ToDto(in OrganizationRow row) => new()
    {
        Id = OrganizationId.Parse(row.OrganizationId),
        OrganizationUserId = row.OrganizationUserId is null
            ? null
            : Guid.Parse(row.OrganizationUserId),
        Name = row.OrganizationName,
        Enabled = row.IsEnabled != 0,
        UseKeyConnector = row.UseKeyConnector != 0,
        Status = row.MemberStatus,
        MemberType = row.MemberType,
        EncryptedOrganizationKey = row.EncryptedOrganizationKey is null
            ? EncString.Empty
            : EncString.FromBytes(row.EncryptedOrganizationKey)
    };

    internal readonly record struct CollectionRow(
        string CollectionId,
        string? OrganizationId,
        int IsReadOnly,
        int CanManage,
        int HidePasswords,
        int? CollectionType,
        byte[] EncryptedName);

    private static VaultCollectionDto ToDto(in CollectionRow row) => new()
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
        byte[]? EncryptedCipherKey,
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

    private static VaultCipherDto ToDto(in CipherRow row, CollectionId[] collectionIds) => new()
    {
        Id = CipherId.Parse(row.CipherId),
        OrganizationId = row.OrganizationId is null
            ? OrganizationId.Empty
            : OrganizationId.Parse(row.OrganizationId),
        FolderId = row.FolderId is null
            ? FolderId.Empty
            : FolderId.Parse(row.FolderId),
        CollectionIds = collectionIds,
        EncryptedKey = row.EncryptedCipherKey is null ? EncString.Empty : EncString.FromBytes(row.EncryptedCipherKey),
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
        Data = []
    };
}
