using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers;
using FluentBitwarden.Contracts.Modules.Vault.Folders;
using FluentBitwarden.Contracts.Modules.Vault.Operations;
using FluentBitwarden.Platform;
using FluentBitwarden.Platform.Ipc;
using FluentBitwarden.Ui.Application;
using FluentBitwarden.Ui.Infrastructure;
using FluentBitwarden.Ui.Infrastructure.Hosting;
using FluentBitwarden.Ui.Infrastructure.RemoteHandlers;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Ui;

public static class Program
{
    [STAThread]
    static int Main()
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        var builder = WinUiHost.CreateSingleInstanced<App>("FluentBitwardenUiSingleInstance");
        builder.Services.AddPlatformServices();
        builder.Services.AddIpcClient(IpcConstants.AppHostPipeName);
        builder.Services.AddIpcEventClient(IpcConstants.AppHostEventsPipeName);

        builder.Services.AddSingleton<IAccountClient, RemoteAccountClient>();
        builder.Services.AddSingleton<IAppSessionClient, RemoteAppSessionClient>();
        builder.Services.AddSingleton<IAccountWindowsHelloIntegrationClient, RemoteWindowsHelloUnlockClient>();
        builder.Services.AddSingleton<IVaultOperationsClient, RemoteVaultOperationsClient>();
        builder.Services.AddSingleton<IVaultCipherClient, RemoteVaultCipherClient>();
        builder.Services.AddSingleton<IVaultFolderClient, RemoteVaultFolderClient>();

        builder.Services.AddIpcServer(IpcConstants.UiPipeName, static _ => { });
        builder.Services.AddUiServices();
        builder.Services.AddHostedService(static sp => sp.GetRequiredService<AppSessionMonitor>());

        var host = builder.Build();
        host.Run();

        return 0;
    }
}
