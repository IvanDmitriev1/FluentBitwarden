using BitwardenApi.Primitives;

namespace FluentBitwarden.ViewModels.Vault.Browse.Models;

public sealed record ShowVaultCipherMessage(string SearchText, VaultCipher SelectedCipher);