using System.Net;
using System.Net.Http.Headers;
using BitwardenApi.Client.Http;
using BitwardenApi.Identity;
using BitwardenApi.Notifications;
using BitwardenApi.Notifications.Abstractions;
using BitwardenApi.Vault.Attachments;
using BitwardenApi.Vault.Ciphers;
using BitwardenApi.Vault.Folders;
using BitwardenApi.Vault.Sync;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace BitwardenApi;

public static class BitwardenApiServiceCollectionExtensions
{
    public static IServiceCollection AddBitwardenApi(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddTransient<AuthorizationHandler>();

        services.AddHttpClient(HttpClientNames.Identity, static client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Bitwarden-Client-Version", "2026.2.1");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddBitwardenReadRetry();

        services.AddHttpClient(HttpClientNames.Vault, static client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Bitwarden-Client-Version", "2026.2.1");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpMessageHandler<AuthorizationHandler>()
            .AddBitwardenReadRetry();

        // The blob download targets pre-signed third-party storage URLs. It deliberately carries no
        // Bitwarden authorization header or client headers so the access token is never disclosed.
        services.AddHttpClient(HttpClientNames.AttachmentDownload, static client =>
            {
                client.Timeout = Timeout.InfiniteTimeSpan;
                client.DefaultRequestVersion = HttpVersion.Version20;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            })
            .AddBitwardenReadRetry();

        services.AddSingleton<IIdentityApi, IdentityApi>();
        services.AddSingleton<IWebAuthnIdentityApi, WebAuthnIdentityApi>();
        services.AddSingleton<IVaultSyncApi, VaultSyncApi>();
        services.AddSingleton<ICiphersApi, CiphersApi>();
        services.AddSingleton<IFoldersApi, FoldersApi>();
        services.AddSingleton<IAttachmentsApi, AttachmentsApi>();

        services.AddSingleton<IServerNotificationsDispatcherFactory, ServerNotificationsDispatcherFactory>();

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

