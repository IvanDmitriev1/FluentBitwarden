using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.Modules.BrowserExtension;

internal static class BrowserExtensionServiceCollectionExtensions
{
    public static IServiceCollection AddBrowserExtensionServices(this IServiceCollection services)
    {
        services.AddSingleton<BrowserExtensionService>();
        return services;
    }
}
