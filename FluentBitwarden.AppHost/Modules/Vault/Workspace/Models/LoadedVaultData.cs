namespace FluentBitwarden.AppHost.Modules.Vault.Workspace.Models;

internal record LoadedVaultData(
    Dictionary<CipherId, VaultCipher> CiphersById,
    List<VaultFolder> Folders,
    List<VaultCollection> Collections);
