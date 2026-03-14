using BitwaredApi.Abstractions;
using BitwaredApi.Services;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

[assembly: Fody.ConfigureAwait(false)]

namespace BitwaredApi;

public static class BitwaredApiServiceCollectionExtensions
{
    public static IServiceCollection AddBitwaredCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<ICryptoService, CryptoService>();
        services.AddSingleton<IAuthenticationWorkflow, AuthenticationWorkflow>();
        services.AddSingleton<ISessionRefreshWorkflow, SessionRefreshWorkflow>();
        services.AddSingleton<IMasterPasswordUnlockWorkflow, MasterPasswordUnlockWorkflow>();
        services.AddSingleton<IVaultSyncService, VaultSyncService>();
        services.AddSingleton<ICipherPayloadDecryptor, CipherPayloadDecryptor>();

        services.AddHttpClient<IIdentityClient, IdentityClient>()
            .AddBitwaredRetryPolicy();

        services.AddHttpClient<IApiClient, ApiClient>()
            .AddBitwaredRetryPolicy();

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
