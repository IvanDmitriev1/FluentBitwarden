using FluentBitwarden.Contracts.Infrastructure.Ipc;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Platform.AppHost.Lifetime;
using FluentBitwarden.Platform.Hosting;
using FluentBitwarden.Views.Accounts.Unlock.Client;
using FluentBitwarden.Views.Settings.Theming;
using FluentBitwarden.Views.SshAgent.UserApproval;
using FluentBitwarden.Views.Vault.Browse.SiteIcons;
using FluentBitwarden.Views.Vault.Client;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Composition;

internal static class UiServiceRegistration
{
    public static IServiceCollection AddUiServices(this IServiceCollection services)
    {
        services.AddSharedHttpClient();

        services.AddSingleton<WindowManager>();
        services.AddSingleton<IWindowManager>(static sp => sp.GetRequiredService<WindowManager>());
        services.AddSingleton<IThemeService>(static sp => sp.GetRequiredService<WindowManager>());
        services.AddSingleton<IUiHostedServiceStarter, UiHostedServiceStarter>();

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
