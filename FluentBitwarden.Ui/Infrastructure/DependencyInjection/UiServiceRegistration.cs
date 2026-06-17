using FluentBitwarden.Contracts.Infrastructure.Ipc;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Infrastructure.Clients;
using FluentBitwarden.Infrastructure.Hosting;
using FluentBitwarden.Infrastructure.SiteIcons;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Infrastructure.DependencyInjection;

internal static class UiServiceRegistration
{
    public static IServiceCollection AddUiServices(this IServiceCollection services)
    {
        services.AddSiteIconHttpClient();

        services.AddSingleton<WindowManager>();
        services.AddSingleton<IWindowManager>(static sp => sp.GetRequiredService<WindowManager>());
        services.AddSingleton<IUiHostedServiceStarter, UiHostedServiceStarter>();

        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<INavigationService>(static sp => sp.GetRequiredService<NavigationService>());

        services.AddSingleton<ISiteIconCache, SiteIconCache>();

        services.AddIpcClient(IpcConstants.AppHostPipeName);
        services.AddSingleton<IAccountsClient, RemoteAccountsClient>();
        services.AddSingleton<IWindowsHelloUnlockClient, RemoteWindowsHelloUnlockClient>();
        services.AddSingleton<IVaultClient, RemoteVaultClient>();

        services.AddIpcServer(IpcConstants.UiPipeName);
        services.AddIpcRequestHandler<UserDialogRequestHandler>();

        return services;
    }
}
