using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.Data;

namespace FluentBitwarden.AppHost.Modules.Vault.KeyResolution;

internal interface IVaultKeyResolverFactory
{
    VaultKeyResolver Create(UnitOfWork unitOfWork, UserKey userKey);
}
