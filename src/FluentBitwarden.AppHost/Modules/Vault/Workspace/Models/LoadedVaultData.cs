namespace FluentBitwarden.AppHost.Modules.Vault.Workspace.Models;

internal record LoadedVaultData(
    Dictionary<CipherId, VaultCipher> CiphersById,
    Dictionary<CollectionId, HashSet<CipherId>> CipherIdsByCollectionId,
    List<VaultFolder> Folders,
    List<VaultCollection> Collections);
