using BitwardenApi;
using FluentBitwarden.AppHost.Application;
using FluentBitwarden.AppHost.Application.Activation;
using FluentBitwarden.AppHost.Infrastructure;
using FluentBitwarden.AppHost.Infrastructure.Abstractions;
using FluentBitwarden.AppHost.Infrastructure.Data;
using FluentBitwarden.AppHost.Modules.Accounts;
using FluentBitwarden.AppHost.Modules.Accounts.ApiAccess.Providers;
using FluentBitwarden.AppHost.Modules.Passkey;
using FluentBitwarden.AppHost.Modules.SshAgent;
using FluentBitwarden.AppHost.Modules.Vault;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32.SafeHandles;
using Microsoft.Windows.AppLifecycle;

namespace FluentBitwarden.AppHost;

internal static class Program
{
    private const string InstanceKey = "FluentBitwardenHostSingleInstance";
    private static SafeFileHandle? _redirectEventHandle;

    [STAThread]
    private static int Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        AppActivationArguments initialActivation = AppInstance.GetCurrent().GetActivatedEventArgs();
        AppInstance keyInstance = AppInstance.FindOrRegisterForKey(InstanceKey);

        if (!keyInstance.IsCurrent)
        {
            RedirectActivationTo(initialActivation, keyInstance);
            return 0;
        }

        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSingleton<AppHostActivationHandler>();
        builder.Services.AddHostedService<AppHostHostedService>();

        builder.Services.AddDatabaseServices();
        builder.Services.AddApplicationInfrastructureServices();

        builder.Services.AddBitwardenApi<BearerAuthTokenProvider>();
        builder.Services.AddAccountModule();
        builder.Services.AddVaultServices();
        builder.Services.AddPasskeyModule();
        builder.Services.AddSshAgent();

        var host = builder.Build();

        keyInstance.Activated += (_, arguments) =>
            host.Services.GetRequiredService<AppHostActivationHandler>().Handle(arguments);

        host.Services
            .GetRequiredService<IHostApplicationLifetime>()
            .ApplicationStarted
            .Register(() => host.Services.GetRequiredService<AppHostActivationHandler>().Handle(initialActivation));

        host.Services.GetRequiredService<IAppSetupService>().Initialize();
        host.Run();
        return 0;
    }

    private static void RedirectActivationTo(AppActivationArguments args, AppInstance keyInstance)
    {
        _redirectEventHandle = PInvoke.CreateEvent(null, bManualReset: true, bInitialState: false, lpName: null);

        _ = Task.Run(() =>
        {
            keyInstance.RedirectActivationToAsync(args).AsTask().Wait();
            PInvoke.SetEvent(_redirectEventHandle);
        });

        const uint CoWaitDefault = 0;
        const uint Infinite = 0xFFFFFFFF;

        HANDLE redirectEventHandle = new(_redirectEventHandle.DangerousGetHandle());
        PInvoke.CoWaitForMultipleObjects(CoWaitDefault, Infinite, [redirectEventHandle], out _);
    }
}
