using System.Diagnostics.CodeAnalysis;
using BitwardenApi;
using FluentBitwarden.AppHost.AppSession;
using FluentBitwarden.AppHost.Hosting;
using FluentBitwarden.AppHost.Infrastructure;
using FluentBitwarden.AppHost.Ipc;
using FluentBitwarden.AppHost.Modules.Account;
using FluentBitwarden.AppHost.Modules.Vault;
using FluentBitwarden.Platform.Diagnostics;
using FluentBitwarden.Platform.Ipc;
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
        var keyInstance = AppInstance.FindOrRegisterForKey(InstanceKey);

        if (!keyInstance.IsCurrent)
        {
            RedirectActivationTo(initialActivation, keyInstance);
            return 0;
        }

        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddSingleton(initialActivation);

#if DEBUG
        builder.ConfigureContainer(new DefaultServiceProviderFactory(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            }));
#endif

        builder.Services.AddHostedService<AppHostLifecycleService>();
        builder.Services.AddAppLogging("apphost");

        builder.Services.AddApplicationInfrastructureServices();
        builder.Services.AddBitwardenApi();
        builder.Services.AddAppSessionModule();
        builder.Services.AddAccountModule();
        builder.Services.AddVaultModule();

        builder.Services.AddIpcServer(
            IpcConstants.AppHostPipeName,
            RegisterIpcHandlers);

        builder.Services.AddIpcEventServer(IpcConstants.AppHostEventsPipeName);
        builder.Services.AddIpcClient(IpcConstants.UiPipeName);

        builder.Services.AddHostedService<AppHostLifecycleService>();

        var host = builder.Build();

        keyInstance.Activated += (_, arguments) =>
            host.Services.GetRequiredService<AppHostLifecycleService>().HandleAppActivation(arguments);

        host.Run();
        return 0;
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification =
            "IPC handler registration is the documented reflection boundary. Each handler is statically registered and its required public members are preserved by Add<THandler>().")]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050",
        Justification =
            "IPC endpoint creation intentionally closes generic endpoint types at registration time.")]
    private static void RegisterIpcHandlers(IpcRpcHandlerBuilder handlers)
    {
        handlers.Add<AppSessionIpcHandler>();
        handlers.Add<AccountIpcHandler>();
        handlers.Add<AccountWindowsHelloIntegrationIpcHandler>();
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
