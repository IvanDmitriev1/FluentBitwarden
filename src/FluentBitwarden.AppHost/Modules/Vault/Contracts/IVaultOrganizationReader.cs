namespace FluentBitwarden.AppHost.Modules.Vault.Contracts;

internal interface IVaultOrganizationReader
{
    string? GetOrganizationName(UserId userId, OrganizationId organizationId);
}
