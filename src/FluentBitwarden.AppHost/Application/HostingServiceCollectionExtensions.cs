using FluentBitwarden.AppHost.Application.Activation;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.Application;

internal static class HostingServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<AppHostActions>();
        services.AddSingleton<AppActivationHandler>();
        services.AddSingleton<AppTray>();

        return services;
    }
}
