using FluentBitwarden.AppHost.Application.Activation;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.Application;

internal static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<AppHostActivationHandler>();
        services.AddHostedService<AppHostHostedService>();

        return services;
    }
}
