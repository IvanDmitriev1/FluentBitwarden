using FluentBitwarden.AppHost.Application.Tray;
using FluentBitwarden.AppHost.Infrastructure.Activation;
using Microsoft.Win32.SafeHandles;
using Microsoft.Windows.AppLifecycle;

namespace FluentBitwarden.AppHost;

internal static class Program
{
    private static SafeFileHandle? _redirectEventHandle;

    [STAThread]
    private static int Main()
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        AppActivationArguments initialActivation = AppInstance.GetCurrent().GetActivatedEventArgs();
        AppInstance keyInstance = AppInstance.FindOrRegisterForKey("FluentBitwardenHostSingleInstance");

        if (!keyInstance.IsCurrent)
        {
            RedirectActivationTo(initialActivation, keyInstance);
            return 0;
        }

        var trayHost = new TrayHost();
        keyInstance.Activated += (_, args) => HandleActivation(trayHost, args);

        HandleActivation(trayHost, initialActivation);
        MSG message;

        while (PInvoke.GetMessage(out message, default, 0, 0).Value > 0)
        {
            PInvoke.TranslateMessage(in message);
            PInvoke.DispatchMessage(in message);
        }

        return unchecked((int)(nuint)message.wParam);
    }

    private static void HandleActivation(TrayHost trayHost, AppActivationArguments args)
    {
        AppLifecycleCommand command = AppLifecycleCommandExtensions.From(args);
        
        if (command == AppLifecycleCommand.Exit)
        {
            trayHost.RequestShutdown();
            return;
        }

        AppProcessLauncher.Activate();
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
