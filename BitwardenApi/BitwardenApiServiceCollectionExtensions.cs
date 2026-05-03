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
                client.Timeout = TimeSpan.FromSeconds(5);
            })
            .AddHttpMessageHandler<BitwardenRequiredHeadersHandler>();

        services.AddHttpClient("BitwardenApiVaultHttpClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(5);
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
