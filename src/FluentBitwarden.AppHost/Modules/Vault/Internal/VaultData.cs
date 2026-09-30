using BitwardenApi.Vault.Items.Contracts;

namespace FluentBitwarden.AppHost.Modules.Vault.Internal;

internal sealed record VaultData(
    List<VaultCipher> Ciphers,
    List<VaultFolder> Folders,
    List<VaultCollection> Collections);
