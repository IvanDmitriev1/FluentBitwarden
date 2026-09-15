namespace FluentBitwarden.AppHost.Modules.Vault.Workspace.Models;

internal readonly record struct VaultCipherKeyMaterial(
    CipherId CipherId,
    OrganizationId OrganizationId,
    EncString ProtectedCipherKey);