namespace FluentBitwarden.Ui.Screens.Shell.Models;

public sealed record VaultCipherSelection(
    string QueryText,
    Cipher SelectedItem);
