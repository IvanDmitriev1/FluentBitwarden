using FluentBitwarden.AppHost.Data.Mapping;

namespace FluentBitwarden.AppHost.Modules.Vault.Persistence.Mapping;

internal static class VaultCollectionMapper
{
    public readonly record struct CollectionRow(
        string CollectionId,
        string? OrganizationId,
        int IsReadOnly,
        int CanManage,
        int HidePasswords,
        int CollectionType,
        byte[] EncryptedName);

    public static VaultCollectionResponse ToDomain(in CollectionRow row) => new()
    {
        Id = CollectionId.Parse(row.CollectionId),
        OrganizationId = SqliteConversions.ParseOrEmpty(row.OrganizationId, OrganizationId.Parse, OrganizationId.Empty),
        ReadOnly = row.IsReadOnly.ToBool(),
        Manage = row.CanManage.ToBool(),
        HidePasswords = row.HidePasswords.ToBool(),
        Type = row.CollectionType,
        EncryptedName = EncString.FromBytes(row.EncryptedName)
    };

    public readonly record struct CollectionInsertParameters(
        string UserId,
        string CollectionId,
        string? OrganizationId,
        int ReadOnly,
        int Manage,
        int HidePasswords,
        int CollectionType,
        byte[] EncryptedName);

    public static CollectionInsertParameters ToInsertParameters(string userId, in VaultCollectionResponse dto) => new(
        UserId: userId,
        CollectionId: dto.Id.ToString(),
        OrganizationId: dto.OrganizationId.IsEmpty
            ? null
            : dto.OrganizationId.ToString(),
        ReadOnly: dto.ReadOnly.ToSqliteInt(),
        Manage: dto.Manage.ToSqliteInt(),
        HidePasswords: dto.HidePasswords.ToSqliteInt(),
        CollectionType: dto.Type,
        EncryptedName: dto.EncryptedName.ToByteArray());
}
