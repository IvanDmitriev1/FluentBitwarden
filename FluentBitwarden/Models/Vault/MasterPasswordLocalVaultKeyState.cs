namespace FluentBitwarden.Models.Vault;

internal sealed record MasterPasswordLocalVaultKeyState(
    string Nonce,
    string Ciphertext,
    string Tag);