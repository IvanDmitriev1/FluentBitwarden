using FluentBitwarden.Shared.Connectivity.Abstractions;
using FluentBitwarden.Shared.Connectivity.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Shared.Connectivity;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConnectivityModule(this IServiceCollection services)
    {
        services.AddSingleton<IConnectivityService, WindowsConnectivityService>();
        return services;
    }
}
