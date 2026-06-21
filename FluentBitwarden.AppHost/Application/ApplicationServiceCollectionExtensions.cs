using FluentBitwarden.AppHost.Application.Activation;
using FluentBitwarden.AppHost.Application.Sessions;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.Application;

internal static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<AppHostActivationHandler>();
        services.AddSingleton<VaultSessionCoordinator>();
        services.AddSingleton<IVaultSessionCoordinator>(
            static serviceProvider => serviceProvider.GetRequiredService<VaultSessionCoordinator>());
        services.AddHostedService<AppHostHostedService>();

        return services;
    }
}
