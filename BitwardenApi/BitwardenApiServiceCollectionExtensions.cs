using BitwardenApi.Identity;
using BitwardenApi.Notifications;
using BitwardenApi.Vault.Attachments;
using BitwardenApi.Vault.Items;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using System.Net;

namespace BitwardenApi;

public static class BitwardenApiServiceCollectionExtensions
{
    public static IServiceCollection AddBitwardenApi(this IServiceCollection services)
    {
        services.AddTransient<BitwardenRequiredHeadersHandler>();
        services.AddTransient<BitwardenAuthorizationHandler>();

        services.AddHttpClient("BitwardenApiIdentityHttpClient", static client =>
            {
                client.Timeout = TimeSpan.FromSeconds(2);
                client.DefaultRequestVersion = HttpVersion.Version20;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            })
            .AddHttpMessageHandler<BitwardenRequiredHeadersHandler>()
            .AddBitwardenReadRetry();

        services.AddHttpClient("BitwardenApiVaultHttpClient", static client =>
            {
                client.Timeout = TimeSpan.FromSeconds(2);
                client.DefaultRequestVersion = HttpVersion.Version20;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            })
            .AddHttpMessageHandler<BitwardenRequiredHeadersHandler>()
            .AddHttpMessageHandler<BitwardenAuthorizationHandler>()
            .AddBitwardenReadRetry();

        // The blob download targets pre-signed third-party storage URLs. It deliberately carries no
        // Bitwarden authorization header or client headers so the access token is never disclosed.
        services.AddHttpClient("BitwardenApiAttachmentDownloadHttpClient", static client =>
            {
                client.Timeout = Timeout.InfiniteTimeSpan;
                client.DefaultRequestVersion = HttpVersion.Version20;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            })
            .AddBitwardenReadRetry();

        services.AddSingleton<IIdentityApi, IdentityApi>();
        services.AddSingleton<IVaultItemsApi, VaultItemsApi>();
        services.AddSingleton<IVaultCipherAttachmentApi, VaultCipherAttachmentApi>();

        services.AddSingleton<IBitwardenNotificationsApi, BitwardenNotificationsApi>();

        return services;
    }

    private static IHttpClientBuilder AddBitwardenReadRetry(this IHttpClientBuilder builder)
    {
        builder.AddResilienceHandler("BitwardenReadRetry", static resilienceBuilder =>
        {
            HttpRetryStrategyOptions retryOptions = new()
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(200),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true
            };

            retryOptions.DisableForUnsafeHttpMethods();
            resilienceBuilder.AddRetry(retryOptions);
        });

        return builder;
    }
}

