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

[assembly: Fody.ConfigureAwait(false)]

namespace BitwardenApi;

public static class BitwardenApiServiceCollectionExtensions
{
    public static IServiceCollection AddBitwardenApi<TAuthHandler>(this IServiceCollection services)
        where TAuthHandler : DelegatingHandler
    {
        services.AddTransient<BitwardenRequiredHeadersHandler>();

        services.AddHttpClient<IIdentityApiClient, IdentityApiClient>()
            .AddHttpMessageHandler<BitwardenRequiredHeadersHandler>();

        services.AddHttpClient<IVaultApiClient, VaultApiClient>()
            .AddHttpMessageHandler<BitwardenRequiredHeadersHandler>()
            .AddHttpMessageHandler<TAuthHandler>();

        services.AddHttpClient<IAttachmentsApiClient, AttachmentsApiClient>()
            .AddHttpMessageHandler<BitwardenRequiredHeadersHandler>()
            .AddHttpMessageHandler<TAuthHandler>();

        services.AddSingleton<INotificationsClient, NotificationsClient>();

        return services;
    }
}
