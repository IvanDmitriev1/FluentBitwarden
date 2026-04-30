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
        services.AddTransient<TAuthHandler>();

        services.AddHttpClient("BitwardenApiIdentityHttpClient", client =>
            {
            })
            .AddHttpMessageHandler<BitwardenRequiredHeadersHandler>()
            .AddHttpMessageHandler<TAuthHandler>();

        services.AddHttpClient("BitwardenApiVaultHttpClient", client =>
            {
                
            })
            .AddHttpMessageHandler<BitwardenRequiredHeadersHandler>()
            .AddHttpMessageHandler<TAuthHandler>();

        services.AddSingleton<IIdentityApiClient, IdentityApiClient>();
        services.AddSingleton<IVaultApiClient, VaultApiClient>();
        services.AddSingleton<IAttachmentsApiClient, AttachmentsApiClient>();

        services.AddSingleton<INotificationsClient, NotificationsClient>();
        services.AddSingleton<INotificationDispatcher, NotificationDispatcher>();

        return services;
    }
}
