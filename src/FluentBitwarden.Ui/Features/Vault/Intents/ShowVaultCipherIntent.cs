namespace FluentBitwarden.Ui.Features.Vault.Intents;

public sealed record ShowVaultCipherIntent(string SearchText, Cipher SelectedCipher) : VaultCipherNavigationIntent;
