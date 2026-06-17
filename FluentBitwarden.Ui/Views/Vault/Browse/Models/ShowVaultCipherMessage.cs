using BitwardenApi.Primitives;

namespace FluentBitwarden.Views.Vault.Browse.Models;

public sealed record ShowVaultCipherMessage(string SearchText, VaultCipher SelectedCipher);