using FluentBitwarden.AppHost.Modules.Vault.Workspace;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Internal;
using FluentBitwarden.Contracts.Infrastructure.Ipc;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.Modules.Vault;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVaultServices(this IServiceCollection services)
    {
        services.AddSingleton<VaultLoader>();
        services.AddSingleton<IVaultSynchronizer, VaultSynchronizer>();

        services.AddSingleton<VaultWorkspace>();
        services.AddSingleton<IVaultWorkspace>(static sp => sp.GetRequiredService<VaultWorkspace>());
        services.AddSingleton<IUnlockedVaultReader>(static sp => sp.GetRequiredService<VaultWorkspace>());

        services.AddIpcRequestHandler<VaultClientHandlers>();
        return services;
    }
}