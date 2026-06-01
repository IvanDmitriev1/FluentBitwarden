using BitwardenApi.Models;

namespace FluentBitwarden.Views.Vault.Browse.Models;

public sealed record ShowVaultCipherMessage(string SearchText, VaultCipher SelectedCipher);