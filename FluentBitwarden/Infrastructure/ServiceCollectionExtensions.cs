using FluentBitwarden.Contracts.Infrastructure.Ipc;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Infrastructure.Abstractions;
using FluentBitwarden.Infrastructure.Implementations;
using FluentBitwarden.Infrastructure.IpcClientsImplementations;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Infrastructure;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSharedHttpClient();

        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<INavigationService>(static sp => sp.GetRequiredService<NavigationService>());

        services.AddSingleton<ISiteIconCache, SiteIconCache>();
        services.AddSingleton<IAppHostLifetimeService, AppHostLifetimeService>();


        services.AddIpcClient(IpcConstants.AppHostPipeName);
        services.AddSingleton<IAccountsClient, RemoteAccountsClient>();
        services.AddSingleton<IWindowsHelloUnlockClient, RemoteWindowsHelloUnlockClient>();
        services.AddSingleton<IVaultClient, RemoteVaultClient>();

        services.AddIpcServer(IpcConstants.UiPipeName);
        services.AddIpcRequestHandler<UserDialogRequestHandler>();

        return services;
    }
}
