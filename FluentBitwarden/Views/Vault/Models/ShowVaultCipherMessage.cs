using BitwardenApi.Models;

namespace FluentBitwarden.Views.Vault.Models;

public sealed record ShowVaultCipherMessage(string SearchText, VaultCipher SelectedCipher);