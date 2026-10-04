using System.Net;
using FluentBitwarden.Platform.Infrastructure.Connectivity;
using FluentBitwarden.Platform.SiteIcons;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Platform;

public static class ServicesExtensions
{
    private const string SiteIconHttpClientName = "SiteIconHttpClient";

    public static IServiceCollection AddPlatformServices(this IServiceCollection services)
    {
        services.AddHttpClient(SiteIconHttpClientName, static client =>
        {
            client.DefaultRequestVersion = HttpVersion.Version20;
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            client.Timeout = TimeSpan.FromSeconds(8);

            client.DefaultRequestHeaders.Add("Accept",
                "image/avif,image/webp,image/apng,image/svg+xml,image/*,*/*;q=0.8");
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36");
        });

        services.AddSingleton<INetworkStatus, WindowsNetworkStatus>();
        services.AddSingleton<ISiteIconCache, SiteIconCache>();
        return services;
    }

    internal static HttpClient CreateSiteIconClient(this IHttpClientFactory factory) =>
        factory.CreateClient(SiteIconHttpClientName);
}
