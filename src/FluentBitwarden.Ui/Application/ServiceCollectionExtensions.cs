using FluentBitwarden.Application.Abstractions;
using FluentBitwarden.Application.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Application;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IAppCoordinator, AppCoordinator>();
        services.AddSingleton<IUiHostedServiceManager, UiHostedServiceManager>();
        services.AddSingleton<IAppSessionResolver, AppSessionResolver>();

        return services;
    }
}