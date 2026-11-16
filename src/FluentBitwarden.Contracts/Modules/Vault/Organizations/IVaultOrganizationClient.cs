namespace FluentBitwarden.Contracts.Modules.Vault.Organizations;

public interface IVaultOrganizationClient
{
    Task<VaultOrganizationNameResponse> GetOrganizationNameAsync(
        GetVaultOrganizationNameRequest request,
        CancellationToken cancellationToken = default);
}
