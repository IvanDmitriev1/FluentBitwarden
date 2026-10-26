
namespace FluentBitwarden.AppHost.Modules.Vault.Internal;

internal readonly record struct VaultCipherKeyMaterial(
    CipherId CipherId,
    OrganizationId OrganizationId,
    EncString ProtectedCipherKey);
