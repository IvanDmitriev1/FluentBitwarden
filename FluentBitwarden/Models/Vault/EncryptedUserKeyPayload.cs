namespace FluentBitwarden.Models.Vault;

internal sealed record EncryptedUserKeyPayload(
    string Nonce,
    string Ciphertext,
    string Tag);
