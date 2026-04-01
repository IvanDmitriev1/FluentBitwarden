using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Windows.Win32;
using Windows.Win32.Foundation;
using WinUIEx;

namespace FluentBitwarden;

public static class Program
{
    private static Microsoft.Win32.SafeHandles.SafeFileHandle? _redirectEventHandle;

    [STAThread]
    static int Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        bool isRedirect = DecideRedirection();

        if (isRedirect)
            return 0;

        var fss = SimpleSplashScreen.ShowDefaultSplashScreen();

        Microsoft.UI.Xaml.Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(
                DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App(fss);
        });

        return 0;
    }

    private static bool DecideRedirection()
    {
        AppInstance keyInstance = AppInstance.FindOrRegisterForKey("FluentBitwardenSingleInstance");
        AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();

        if (keyInstance.IsCurrent)
        {
            keyInstance.Activated += OnActivated;
            return false;
        }

        RedirectActivationTo(args, keyInstance);
        return true;
    }

    // Do the redirection on another thread, and use a non-blocking
    // wait method to wait for the redirection to complete.
    public static void RedirectActivationTo(AppActivationArguments args, AppInstance keyInstance)
    {
        _redirectEventHandle = PInvoke.CreateEvent(null, bManualReset: true, bInitialState: false, lpName: null);

        Task.Run(() =>
        {
            keyInstance.RedirectActivationToAsync(args).AsTask().Wait();
            PInvoke.SetEvent(_redirectEventHandle);
        });

        const uint CWMO_DEFAULT = 0;
        const uint INFINITE = 0xFFFFFFFF;

        HANDLE rawHandle = new(_redirectEventHandle.DangerousGetHandle());
        PInvoke.CoWaitForMultipleObjects(CWMO_DEFAULT, INFINITE, [rawHandle], out _);
    }

    private static void OnActivated(object? sender, AppActivationArguments args)
    {
        App currentApp = App.Current;
        currentApp.ReopenWindow();
    }
}
