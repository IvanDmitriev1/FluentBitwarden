using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers;
using FluentBitwarden.Contracts.Modules.Vault.Folders;
using FluentBitwarden.Contracts.Modules.Vault.Operations;
using FluentBitwarden.Platform;
using FluentBitwarden.Platform.Ipc;
using FluentBitwarden.Ui.Infrastructure.Hosting;
using FluentBitwarden.Ui.Infrastructure.RemoteHandlers;
using FluentBitwarden.Ui.Screens.Loading;
using FluentBitwarden.Ui.Screens.Setup;
using FluentBitwarden.Ui.Screens.Setup.Steps.Email;
using FluentBitwarden.Ui.Screens.Setup.Steps.Password;
using FluentBitwarden.Ui.Screens.Setup.Steps.TwoFactor;
using FluentBitwarden.Ui.Screens.Shell;
using FluentBitwarden.Ui.Screens.Shell.Controls;
using FluentBitwarden.Ui.Screens.Unlock;
using FluentBitwarden.Ui.Features.Settings;
using FluentBitwarden.Ui.Features.Vault;
using Microsoft.Extensions.DependencyInjection;
using Navis.WinUI.Extensions;
using FluentBitwarden.Ui.Application;
using FluentBitwarden.Ui.Application.Abstractions;
using FluentBitwarden.Ui.Windows;
using FluentBitwarden.Ui.Windows.Abstractions;

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

        builder.Services.AddIpcServer(IpcConstants.UiPipeName, static _ => { });

        builder.Services.AddSingleton<IWindowManager, WindowManager>();
        builder.Services.AddSingleton<IAppRouter, AppRouter>();

        builder.Services.AddSingleton<AppSessionMonitor>();
        builder.Services.AddSingleton<IAppSession>(static sp => sp.GetRequiredService<AppSessionMonitor>());
        builder.Services.AddHostedService(static sp => sp.GetRequiredService<AppSessionMonitor>());


        builder.Services.AddTransient<ISetupNavigator, SetupNavigator>();
        builder.Services.AddTransient<LoadingPageViewModel>();
        builder.Services.AddTransient<SetupEmailPageViewModel>();
        builder.Services.AddTransient<SetupPasswordPageViewModel>();
        builder.Services.AddTransient<SetupTwoFactorPageViewModel>();
        builder.Services.AddTransient<UnlockPageViewModel>();
        builder.Services.AddTransient<ShellPageViewModel>();
        builder.Services.AddTransient<SettingsPageViewModel>();
        builder.Services.AddTransient<VaultPageViewModel>();
        builder.Services.AddTransient<VaultCipherSearchBoxViewModel>();

        var host = builder.Build();
        host.Run();

        return 0;
    }
}
