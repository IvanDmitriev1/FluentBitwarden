using BitwaredApi.Abstractions;
using FluentBitwarden.Core.Abstractions;
using FluentBitwarden.Security;
using FluentBitwarden.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Extentions;

internal static class BitwaredPlatformServiceCollectionExtensions
{
    public static IServiceCollection AddBitwaredPlatformServices(this IServiceCollection services)
    {
        services.AddSingleton<IAppPaths, AppPaths>();
        services.AddSingleton<IDeviceInfoProvider, LocalDeviceInfoProvider>();
        services.AddSingleton<ISessionStore, DpapiSessionStore>();
        services.AddSingleton<IVaultCache, SqliteVaultCache>();

        return services;
    }
}
