namespace FluentBitwarden.Contracts.Modules.Vault.Folders;

public interface IVaultFolderClient
{
    Task<VaultFolder[]> GetFoldersAsync(
        GetVaultFoldersRequest request,
        CancellationToken cancellationToken = default);
}
