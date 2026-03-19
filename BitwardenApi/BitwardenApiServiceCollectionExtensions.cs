using BitwardenApi.Attachments;
using BitwardenApi.Identity;
using BitwardenApi.Notifications;
using BitwardenApi.Vault;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BitwardenApi;

public static class BitwardenApiServiceCollectionExtensions
{
    public static IServiceCollection AddBitwardenApi(this IServiceCollection services)
    {
        services.AddHttpClient<IIdentityApiClient, IdentityApiClient>();
        services.AddHttpClient<IVaultApiClient, VaultApiClient>();
        services.AddHttpClient<IAttachmentsApiClient, AttachmentsApiClient>();
        services.AddSingleton<INotificationsClient, NotificationsClient>();

        return services;
    }
}
