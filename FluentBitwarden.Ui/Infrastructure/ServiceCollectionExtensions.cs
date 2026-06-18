using FluentBitwarden.Contracts.Ipc;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Infrastructure.AppLifecycle;
using FluentBitwarden.Infrastructure.Clients;
using FluentBitwarden.Services.Navigation;
using FluentBitwarden.Services.Notifications;
using FluentBitwarden.Services.SiteIcons;
using FluentBitwarden.Services.UserDialogs;
using FluentBitwarden.Services.Window;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Infrastructure;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUiServices(this IServiceCollection services)
    {
        services.AddSingleton<WindowManager>();
        services.AddSingleton<IWindowManager>(static sp => sp.GetRequiredService<WindowManager>());
        services.AddSingleton<IUiHostedServiceStarter, UiHostedServiceStarter>();

        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<INavigationService>(static sp => sp.GetRequiredService<NavigationService>());

        services.AddSiteIconHttpClient();
        services.AddSingleton<ISiteIconCache, SiteIconCache>();

        services.AddIpcClient(IpcConstants.AppHostPipeName);
        services.AddSingleton<IAccountsClient, RemoteAccountsClient>();
        services.AddSingleton<IWindowsHelloUnlockClient, RemoteWindowsHelloUnlockClient>();
        services.AddSingleton<IVaultClient, RemoteVaultClient>();

        services.AddIpcServer(IpcConstants.UiPipeName);
        services.AddSingleton<UiDialogDispatcher>();
        services.AddIpcRequestHandler<SshUserActionDialogRequestHandler>();
        services.AddIpcRequestHandler<PasskeyCredentialSelectionRequestHandler>();

        return services;
    }
}
