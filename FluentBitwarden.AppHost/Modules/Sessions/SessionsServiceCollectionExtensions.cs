using FluentBitwarden.AppHost.Modules.Sessions.Abstractions;
using FluentBitwarden.AppHost.Modules.Sessions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.Modules.Sessions;

internal static class SessionsServiceCollectionExtensions
{
    public static IServiceCollection AddSessionServices(this IServiceCollection services)
    {
        services.AddSingleton<VaultSessionManager>();
        services.AddSingleton<IVaultSessionManager>(
            static serviceProvider => serviceProvider.GetRequiredService<VaultSessionManager>());
        services.AddSingleton<IVaultSessionUnlockDialog, VaultSessionUnlockDialog>();

        return services;
    }
}
