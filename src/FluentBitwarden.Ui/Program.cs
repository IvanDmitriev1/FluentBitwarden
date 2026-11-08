using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers;
using FluentBitwarden.Contracts.Modules.Vault.Folders;
using FluentBitwarden.Contracts.Modules.Vault.Operations;
using FluentBitwarden.Platform;
using FluentBitwarden.Platform.Ipc;
using FluentBitwarden.Ui.Application;
using FluentBitwarden.Ui.Application.Abstractions;
using FluentBitwarden.Ui.Infrastructure.Extensions;
using FluentBitwarden.Ui.Infrastructure.Hosting;
using FluentBitwarden.Ui.Infrastructure.RemoteHandlers;
using FluentBitwarden.Ui.Screens;
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
        builder.Services.AddSingleton<IAccountWindowsHelloIntegrationClient, RemoteWindowsHelloUnlockClient>();
        builder.Services.AddSingleton<IVaultOperationsClient, RemoteVaultOperationsClient>();
        builder.Services.AddSingleton<IVaultCipherClient, RemoteVaultCipherClient>();
        builder.Services.AddSingleton<IVaultFolderClient, RemoteVaultFolderClient>();

        builder.Services.AddIpcServer(IpcConstants.UiPipeName, handlers =>
            handlers.Add<UserDialogRequestHandler>());

        builder.Services.AddHostedSingleton<ApplicationController, IApplicationController>();
        builder.Services.AddSingleton<AppRootViewModel>();

        var host = builder.Build();
        host.Run();

        return 0;
    }
}
