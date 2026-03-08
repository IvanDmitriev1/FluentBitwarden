namespace FluentBitwarden.Models.Vault;

internal sealed record LocalVaultUnlockerState(
    string AccountId,
    EncryptedLocalVaultPayload? Payload,
    MasterPasswordLocalVaultKeyState? MasterPassword,
    WindowsHelloLocalVaultKeyState? WindowsHello,
    PinLocalVaultKeyState? Pin);