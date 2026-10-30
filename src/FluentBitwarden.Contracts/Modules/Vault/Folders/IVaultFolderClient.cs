namespace FluentBitwarden.Contracts.Modules.Vault.Folders;

public interface IVaultFolderClient
{
    Task<Folder[]> GetFoldersAsync(
        GetVaultFoldersRequest request,
        CancellationToken cancellationToken = default);
}
