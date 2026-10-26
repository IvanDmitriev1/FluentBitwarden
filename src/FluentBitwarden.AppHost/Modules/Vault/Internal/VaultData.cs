
namespace FluentBitwarden.AppHost.Modules.Vault.Internal;

internal sealed record VaultData(
    List<Cipher> Ciphers,
    List<Folder> Folders,
    List<Collection> Collections);
