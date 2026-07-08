using FluentBitwarden.AppHost.Data.Mapping;

namespace FluentBitwarden.AppHost.Modules.Vault.Persistence.Mapping;

internal static class VaultOrganizationMapper
{
    public readonly record struct OrganizationRow(
        string UserId,
        string OrganizationId,
        string? OrganizationUserId,
        string OrganizationName,
        int IsEnabled,
        int AccessSecretsManager,
        int? MemberStatus,
        byte[]? ProtectedOrganizationKey);

    public static VaultOrganizationResponse ToDomain(in OrganizationRow row) => new()
    {
        Id = OrganizationId.Parse(row.OrganizationId),
        UserId = UserId.Parse(row.UserId),
        OrganizationUserId = row.OrganizationUserId is null
            ? Guid.Empty
            : Guid.Parse(row.OrganizationUserId),
        Name = row.OrganizationName,
        Enabled = row.IsEnabled.ToBool(),
        AccessSecretsManager = row.AccessSecretsManager.ToBool(),
        Status = row.MemberStatus ?? -1,
        ProtectedOrganizationKey = row.ProtectedOrganizationKey is null
            ? AsymmetricEncString.Empty
            : AsymmetricEncString.FromBytes(row.ProtectedOrganizationKey)
    };

    public readonly record struct OrganizationInsertParameters(
        string UserId,
        string OrganizationId,
        string? OrganizationUserId,
        string OrganizationName,
        int IsEnabled,
        int AccessSecretsManager,
        int MemberStatus,
        byte[]? ProtectedOrganizationKey);

    public static OrganizationInsertParameters ToInsertParameters(string userId, in VaultOrganizationResponse dto) => new(
        UserId: userId,
        OrganizationId: dto.Id.ToString(),
        OrganizationUserId: dto.OrganizationUserId == Guid.Empty
            ? null
            : dto.OrganizationUserId.ToString(),
        OrganizationName: dto.Name,
        IsEnabled: dto.Enabled.ToSqliteInt(),
        AccessSecretsManager: dto.AccessSecretsManager.ToSqliteInt(),
        MemberStatus: dto.Status,
        ProtectedOrganizationKey: dto.ProtectedOrganizationKey.IsEmpty
            ? null
            : dto.ProtectedOrganizationKey.ToByteArray());
}
