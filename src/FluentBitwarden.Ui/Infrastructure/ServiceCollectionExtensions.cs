using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Infrastructure.Clients;
using FluentBitwarden.Infrastructure.Notifications;
using FluentBitwarden.Infrastructure.UserDialogs;
using FluentBitwarden.Infrastructure.UserDialogs.Abstractions;
using FluentBitwarden.Infrastructure.Window;
using FluentBitwarden.Platform.Ipc;
using FluentBitwarden.Platform.SiteIcons;
using Microsoft.Extensions.DependencyInjection;
using Navis.WinUI.Extensions;

namespace FluentBitwarden.Infrastructure;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUiServices(this IServiceCollection services)
    {
        services.AddNavisNavigation();
        services.AddSingleton<WindowManager>();
        services.AddSingleton<IWindowManager>(static sp => sp.GetRequiredService<WindowManager>());
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSiteIconCache();

        services.AddIpcClient(IpcConstants.AppHostPipeName);
        services.AddIpcEventClient(IpcConstants.AppHostEventsPipeName);
        services.AddSingleton<IAccountClient, RemoteAccountClient>();
        services.AddSingleton<IAppSessionClient, RemoteAppSessionClient>();
        services.AddSingleton<IAccountWindowsHelloIntegrationClient, RemoteWindowsHelloUnlockClient>();
        services.AddSingleton<IVaultClient, RemoteVaultClient>();

        services.AddIpcServer(
            IpcConstants.UiPipeName,
            handlers => handlers
                .Add<SshUserActionDialogRequestHandler>()
                .Add<PasskeySelectionDialogRequestHandler>());

        services.AddSingleton<IUiDialogCoordinator, UiDialogCoordinator>();

        return services;
    }
}
