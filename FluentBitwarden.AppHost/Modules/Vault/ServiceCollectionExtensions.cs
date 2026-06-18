using FluentBitwarden.AppHost.Modules.BrowserExtension;
using FluentBitwarden.AppHost.Modules.Vault.Workspace;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Internal;
using FluentBitwarden.Contracts.Ipc;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.AppHost.Modules.Vault;

internal static class ServiceCollectionExtensions
{
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "IPC registration intentionally reflects over known AppHost vault handler methods.")]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050",
        Justification = "IPC registration intentionally closes known AppHost vault handler invoker types at startup.")]
    public static IServiceCollection AddVaultServices(this IServiceCollection services)
    {
        services.AddSingleton<VaultLoader>();
        services.AddSingleton<IVaultSynchronizer, VaultSynchronizer>();

        services.AddSingleton<VaultWorkspace>();
        services.AddSingleton<IVaultWorkspace>(static sp => sp.GetRequiredService<VaultWorkspace>());
        services.AddSingleton<IUnlockedVaultReader>(static sp => sp.GetRequiredService<VaultWorkspace>());

        services.AddIpcRequestHandler<VaultClientHandlers>();
        services.AddIpcRequestHandler<BrowserExtensionClientHandlers>();
        return services;
    }
}
