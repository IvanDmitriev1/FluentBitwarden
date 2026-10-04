using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Persistence;
using FluentBitwarden.AppHost.Modules.Vault.Services;
using FluentBitwarden.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.Modules.Vault;

internal static class VaultModuleServiceCollection
{
    public static void AddVaultModule(this IServiceCollection services)
    {
        services.AddPlatformServices();
        services.AddScoped<VaultReaderRepository>();
        services.AddScoped<VaultSyncStateRepository>();
        services.AddScoped<VaultWriterRepository>();

        services.AddScoped<IVaultManager, VaultManager>();
    }
}
