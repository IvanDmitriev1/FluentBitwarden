using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers;
using FluentBitwarden.Contracts.Modules.Vault.Folders;
using FluentBitwarden.Contracts.Modules.Vault.Operations;
using FluentBitwarden.Platform;
using FluentBitwarden.Platform.Ipc;
using FluentBitwarden.Ui.Infrastructure.Hosting;
using FluentBitwarden.Ui.Infrastructure.RemoteHandlers;
using FluentBitwarden.Ui.Infrastructure.Windowing;
using FluentBitwarden.Ui.Screens;
using FluentBitwarden.Ui.Screens.Setup;
using CommunityToolkit.Mvvm.Messaging;
using FluentBitwarden.Ui.Features.Settings;
using FluentBitwarden.Ui.Infrastructure.Windowing.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Navis.WinUI.Extensions;

namespace FluentBitwarden.Ui;

public static class Program
{
    [STAThread]
    static int Main()
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        var builder = WinUiHost.CreateSingleInstanced<App>("FluentBitwardenUiSingleInstance");
        builder.Services.AddNavisNavigation();
        builder.Services.AddPlatformServices();
        builder.Services.AddIpcClient(IpcConstants.AppHostPipeName);
        builder.Services.AddIpcEventClient(IpcConstants.AppHostEventsPipeName);

        builder.Services.AddSingleton<IAccountClient, RemoteAccountClient>();
        builder.Services.AddSingleton<IAppSessionClient, RemoteAppSessionClient>();
        builder.Services.AddSingleton<IMessenger, WeakReferenceMessenger>();
        builder.Services.AddSingleton<IAccountWindowsHelloIntegrationClient, RemoteWindowsHelloUnlockClient>();
        builder.Services.AddSingleton<IVaultOperationsClient, RemoteVaultOperationsClient>();
        builder.Services.AddSingleton<IVaultCipherClient, RemoteVaultCipherClient>();
        builder.Services.AddSingleton<IVaultFolderClient, RemoteVaultFolderClient>();

        builder.Services.AddIpcServer(IpcConstants.UiPipeName, handlers =>
            handlers.Add<UserDialogRequestHandler>());

        builder.Services.AddSingleton<IWindowManager, WindowManager>();
        builder.Services.AddSingleton<AppRootViewModel>();
        builder.Services.AddHostedService(static serviceProvider =>
            serviceProvider.GetRequiredService<AppRootViewModel>());

        builder.Services.AddTransient<SetupViewModel>();
        builder.Services.AddTransient<SettingsPageViewModel>();

        var host = builder.Build();
        host.Run();

        return 0;
    }
}
