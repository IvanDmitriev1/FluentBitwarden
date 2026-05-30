using FluentBitwarden.AppHost.Modules.Vault.Services;
using FluentBitwarden.Contracts.Ipc;
using FluentBitwarden.Contracts.Vault.Abstractions;
using FluentBitwarden.Modules.Vault.Abstractions;
using FluentBitwarden.Modules.Vault.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Modules.Vault;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVaultServices(this IServiceCollection services)
    {
        services.AddSingleton<IVaultService, VaultService>();

        services.AddIpcRequestHandler<VaultClientHandlers>();

        return services;
    }
}