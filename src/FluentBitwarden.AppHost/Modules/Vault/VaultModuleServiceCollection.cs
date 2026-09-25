using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.Modules.Vault;

internal static class VaultModuleServiceCollection
{
    public static void AddVaultModule(this IServiceCollection services)
    {
        services.AddSingleton<IVaultManager, VaultManager>();
    }
}
