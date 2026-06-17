using BitwardenApi.Identity;
using BitwardenApi.Notifications;
using BitwardenApi.Vault.Attachments;
using BitwardenApi.Vault.Items;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace BitwardenApi;

public static class BitwardenApiServiceCollectionExtensions
{
    public static IServiceCollection AddBitwardenApi<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TAuthHandler>(this IServiceCollection services)
        where TAuthHandler : DelegatingHandler
    {
        services.AddTransient<BitwardenRequiredHeadersHandler>();
        services.AddTransient<TAuthHandler>();

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
            .AddHttpMessageHandler<TAuthHandler>()
            .AddBitwardenReadRetry();

        services.AddSingleton<IIdentityApi, IdentityApi>();
        services.AddSingleton<IVaultItemsApi, VaultItemsApi>();
        services.AddSingleton<IVaultAttachmentsApi, VaultAttachmentsApi>();

        services.AddSingleton<INotificationsApi, NotificationsApi>();

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

