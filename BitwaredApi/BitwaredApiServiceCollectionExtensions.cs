using BitwaredApi.Abstractions;
using BitwaredApi.Crypto.Enc;
using BitwaredApi.Services;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace BitwaredApi;

public static class BitwaredApiServiceCollectionExtensions
{
    public static IServiceCollection AddBitwaredCoreServices(
        this IServiceCollection services,
        BitwardenEnvironment defaultEnvironment)
    {
        services.AddSingleton<IEnvironmentConfig>(_ => new EnvironmentConfig(defaultEnvironment));
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ICryptoService, CryptoService>();
        services.AddTransient<AuthHeaderHandler>();

        services.AddHttpClient<IIdentityClient, IdentityClient>()
            .AddBitwaredRetryPolicy();

        services.AddHttpClient<IApiClient, ApiClient>()
            .AddBitwaredRetryPolicy()
            .AddHttpMessageHandler<AuthHeaderHandler>();

        return services;
    }

    public static IHttpClientBuilder AddBitwaredRetryPolicy(this IHttpClientBuilder builder)
    {
        return builder.AddPolicyHandler(CreateRetryPolicy());
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy()
        => HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                2,
                retryAttempt => TimeSpan.FromMilliseconds(150 * retryAttempt));
}
