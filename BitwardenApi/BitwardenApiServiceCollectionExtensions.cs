using BitwardenApi.Modules.Attachments.Abstractions;
using BitwardenApi.Modules.Attachments.Services;
using BitwardenApi.Modules.Identity.Abstractions;
using BitwardenApi.Modules.Identity.Services;
using BitwardenApi.Modules.Notifications.Abstractions;
using BitwardenApi.Modules.Notifications.Services;
using BitwardenApi.Modules.Vault.Abstractions;
using BitwardenApi.Modules.Vault.Services;
using BitwardenApi.Shared.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using System.Diagnostics.CodeAnalysis;

[assembly: Fody.ConfigureAwait(false)]

namespace BitwardenApi;

public static class BitwardenApiServiceCollectionExtensions
{
    public static IServiceCollection AddBitwardenApi<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TAuthHandler>(this IServiceCollection services)
        where TAuthHandler : DelegatingHandler
    {
        services.AddTransient<BitwardenRequiredHeadersHandler>();
        services.AddTransient<TAuthHandler>();

        services.AddHttpClient("BitwardenApiIdentityHttpClient", client =>
            {
                //client.Timeout = TimeSpan.FromSeconds(5);
            })
            .AddHttpMessageHandler<BitwardenRequiredHeadersHandler>()
            .AddBitwardenReadRetry();

        services.AddHttpClient("BitwardenApiVaultHttpClient", client =>
            {
                //client.Timeout = TimeSpan.FromSeconds(5);
            })
            .AddHttpMessageHandler<BitwardenRequiredHeadersHandler>()
            .AddHttpMessageHandler<TAuthHandler>()
            .AddBitwardenReadRetry();

        services.AddSingleton<IIdentityApiClient, IdentityApiClient>();
        services.AddSingleton<IVaultApiClient, VaultApiClient>();
        services.AddSingleton<IAttachmentsApiClient, AttachmentsApiClient>();

        services.AddSingleton<INotificationsClient, NotificationsClient>();
        services.AddSingleton<INotificationDispatcher, NotificationDispatcher>();

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
