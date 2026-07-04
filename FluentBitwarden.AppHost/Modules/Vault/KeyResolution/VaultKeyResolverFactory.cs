using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.Data;

namespace FluentBitwarden.AppHost.Modules.Vault.KeyResolution;

internal sealed class VaultKeyResolverFactory : IVaultKeyResolverFactory
{
    public VaultKeyResolver Create(UnitOfWork unitOfWork, UserKey userKey)
    {
        var keyMaterial = unitOfWork.AccountKeyMaterialRepository.GetById(userKey.UserId) ??
                          throw new InvalidOperationException($"Account key material not found for user '{userKey.UserId}'.");

        var organizations = unitOfWork.VaultReaderRepository.GetAllOrganizations(userKey.UserId);
        return new VaultKeyResolver(userKey, keyMaterial.ProtectedPrivateKey, organizations);
    }
}
