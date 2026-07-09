using FluentBitwarden.AppHost.Application.Activation;
using FluentBitwarden.AppHost.Application.Sessions;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.Application;

internal static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<AppHostActivationHandler>();
        services.AddSingleton<SessionStore>();
        services.AddSingleton<VaultSession>();
        services.AddSingleton<IVaultSession>(
            static serviceProvider => serviceProvider.GetRequiredService<VaultSession>());
        services.AddSingleton<IUnlockedVaultReader>(
            static serviceProvider => serviceProvider.GetRequiredService<SessionStore>());
        services.AddHostedService<AppHostHostedService>();

        return services;
    }
}
