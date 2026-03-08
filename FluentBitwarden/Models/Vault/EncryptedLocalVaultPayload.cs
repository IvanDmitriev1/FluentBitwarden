namespace FluentBitwarden.Models.Vault;

internal sealed record EncryptedLocalVaultPayload(
    string Nonce,
    string Ciphertext,
    string Tag);