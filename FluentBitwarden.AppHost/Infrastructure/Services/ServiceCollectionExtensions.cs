using FluentBitwarden.AppHost.Infrastructure.Services.Abstractions;
using FluentBitwarden.AppHost.Infrastructure.Services.Implementations;
using FluentBitwarden.Infrastructure.Services.Abstractions;
using FluentBitwarden.Infrastructure.Services.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Infrastructure.Services;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationInfrastructureServices(this IServiceCollection services)
    {
        services.AddHttpClient("SharedHttpClient", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(8);
            client.DefaultRequestHeaders.Add("Accept", "image/avif,image/webp,image/apng,image/svg+xml,image/*,*/*;q=0.8");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36");
        });

        services.AddSingleton<IConnectivityService, WindowsConnectivityService>();
        services.AddSingleton<ISiteIconCache, SiteIconCache>();

        services.AddTransient<IAppSetupService, AppSetupService>();

        return services;
    }
}
