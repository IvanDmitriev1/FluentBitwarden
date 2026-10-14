using FluentBitwarden.AppHost.Hosting.Activation;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.Hosting;

internal static class HostingServiceCollectionExtensions
{
    public static IServiceCollection AddAppHostHosting(this IServiceCollection services)
    {
        services.AddSingleton<AppHostActions>();
        services.AddSingleton<AppActivationHandler>();
        services.AddSingleton<AppTray>();

        services.AddHostedService<AppHostLifecycleService>();

        return services;
    }
}
