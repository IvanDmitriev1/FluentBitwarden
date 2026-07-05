using FluentBitwarden.AppHost.Modules.Vault.Attachments;
using FluentBitwarden.AppHost.Modules.Vault.KeyResolution;
using FluentBitwarden.AppHost.Modules.Vault.Workspace;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.Modules.Vault;

internal static class VaultServiceCollectionExtensions
{
    public static IServiceCollection AddVaultServices(this IServiceCollection services)
    {
        services.AddSingleton<VaultLoader>();

        // Register VaultSynchronizer with Lazy<T> to break circular dependency
        services.AddSingleton<VaultSynchronizer>();

        services.AddSingleton<IVaultKeyResolverFactory, VaultKeyResolverFactory>();

        services.AddSingleton<VaultWorkspace>();
        services.AddSingleton<IVaultWorkspace>(
            static serviceProvider => serviceProvider.GetRequiredService<VaultWorkspace>());
        services.AddSingleton<IUnlockedVaultReader>(
            static serviceProvider => serviceProvider.GetRequiredService<VaultWorkspace>());

        services.AddSingleton<IVaultCipherAttachmentDownloadService, VaultCipherAttachmentDownloadService>();

        return services;
    }
}
