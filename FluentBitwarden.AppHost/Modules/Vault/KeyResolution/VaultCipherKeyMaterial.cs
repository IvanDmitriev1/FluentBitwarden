namespace FluentBitwarden.AppHost.Modules.Vault.KeyResolution;

internal readonly record struct VaultCipherKeyMaterial(
    CipherId CipherId,
    OrganizationId OrganizationId,
    EncString ProtectedCipherKey);