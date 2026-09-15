using AsyncAwaitBestPractices;
using BitwardenApi;
using FluentBitwarden.AppHost.Application;
using FluentBitwarden.AppHost.Application.Activation;
using FluentBitwarden.AppHost.Infrastructure;
using FluentBitwarden.AppHost.Infrastructure.Services;
using FluentBitwarden.AppHost.Modules.Accounts;
using FluentBitwarden.AppHost.Modules.BrowserExtension;
using FluentBitwarden.AppHost.Modules.Passkey;
using FluentBitwarden.AppHost.Modules.Sessions;
using FluentBitwarden.AppHost.Modules.SshAgent;
using FluentBitwarden.AppHost.Modules.Vault;
using FluentBitwarden.Platform.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        var keyInstance = AppInstance.FindOrRegisterForKey(InstanceKey);

        if (!keyInstance.IsCurrent)
        {
            RedirectActivationTo(initialActivation, keyInstance);
            return 0;
        }

        var builder = Host.CreateApplicationBuilder(args);

#if DEBUG
        builder.ConfigureContainer(new DefaultServiceProviderFactory(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            }));
#endif

        builder.Services.AddAppLogging("apphost");

        builder.Services.AddApplicationServices();
        builder.Services.AddApplicationInfrastructureServices();

        builder.Services.AddBitwardenApi();
        builder.Services.AddAccountServices();
        builder.Services.AddVaultServices();
        builder.Services.AddSessionServices();
        builder.Services.AddBrowserExtensionServices();
        builder.Services.AddPasskeyServices();
        builder.Services.AddSshAgent();

        builder.Services.AddAppHostIpc();

        var host = builder.Build();

        var logger = host.Services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("FluentBitwarden.AppHost");

        SafeFireAndForgetExtensions.SetDefaultExceptionHandling(logger.UnhandledException);

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
