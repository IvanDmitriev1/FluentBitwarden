using BitwaredApi.Http.DelegatingHandlers;
using BitwaredApi.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace BitwaredApi.Http;

internal static class HttpClientFactoryExtensions
{
    public static IServiceCollection AddBitwaredHttpClients(this IServiceCollection services, BitwaredApiOptions options)
    {
        services.AddTransient<RetryHandler>();
        services.AddTransient<AuthHeaderHandler>();

        IHttpClientBuilder identityClient = services.AddHttpClient<IdentityClient>((serviceProvider, client) =>
        {
        });

        if (options.HttpMessageHandlerFactory is not null)
        {
            identityClient.ConfigurePrimaryHttpMessageHandler(options.HttpMessageHandlerFactory);
        }

        identityClient.AddHttpMessageHandler<RetryHandler>();

        IHttpClientBuilder apiClient = services.AddHttpClient<ApiClient>((serviceProvider, client) =>
        {
        });

        if (options.HttpMessageHandlerFactory is not null)
        {
            apiClient.ConfigurePrimaryHttpMessageHandler(options.HttpMessageHandlerFactory);
        }

        apiClient
            .AddHttpMessageHandler<RetryHandler>()
            .AddHttpMessageHandler<AuthHeaderHandler>();

        return services;
    }
}
