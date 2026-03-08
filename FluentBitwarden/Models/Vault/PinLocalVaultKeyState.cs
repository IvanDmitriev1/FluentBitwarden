namespace FluentBitwarden.Models.Vault;

internal sealed record PinLocalVaultKeyState(
    int Iterations,
    string Salt,
    string Nonce,
    string Ciphertext,
    string Tag);