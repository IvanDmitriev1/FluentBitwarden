using FluentBitwarden.Modules.Connectivity.Abstractions;
using FluentBitwarden.Modules.Connectivity.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Modules.Connectivity;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConnectivityModule(this IServiceCollection services)
    {
        services.AddSingleton<IConnectivityService, WindowsConnectivityService>();
        return services;
    }
}
