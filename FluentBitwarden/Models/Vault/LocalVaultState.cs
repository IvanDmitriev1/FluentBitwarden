namespace FluentBitwarden.Models.Vault;

internal sealed record LocalVaultState(
    string AccountId,
    EncryptedUserKeyPayload? Payload,
    MasterPasswordLocalVaultKeyState? MasterPassword,
    WindowsHelloLocalVaultKeyState? WindowsHello,
    PinLocalVaultKeyState? Pin);
