using FluentBitwarden.Platform.Ipc;
using FluentBitwarden.Platform.SiteIcons;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Infrastructure.Clients;
using Microsoft.Extensions.DependencyInjection;
using FluentBitwarden.Infrastructure.Window;
using FluentBitwarden.Infrastructure.Notifications;
using FluentBitwarden.Infrastructure.UserDialogs;

namespace FluentBitwarden.Infrastructure;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUiServices(this IServiceCollection services)
    {
        services.AddSingleton<WindowManager>();
        services.AddSingleton<IWindowManager>(static sp => sp.GetRequiredService<WindowManager>());
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSiteIconCache();

        services.AddIpcClient(IpcConstants.AppHostPipeName);
        services.AddIpcEventClient(IpcConstants.AppHostEventsPipeName);
        services.AddSingleton<IAccountsClient, RemoteAccountsClient>();
        services.AddSingleton<IWindowsHelloUnlockClient, RemoteWindowsHelloUnlockClient>();
        services.AddSingleton<IVaultClient, RemoteVaultClient>();

        services.AddIpcServer(
            IpcConstants.UiPipeName,
            handlers => handlers
                .Add<SshUserActionDialogRequestHandler>()
                .Add<PasskeyCredentialSelectionRequestHandler>());

        services.AddSingleton<UiDialogDispatcher>();

        return services;
    }
}
