namespace FluentBitwarden.ViewModels.Vault.Models;

public sealed record ShowVaultCipherIntent(string SearchText, Cipher SelectedCipher) : VaultCipherNavigationIntent;
