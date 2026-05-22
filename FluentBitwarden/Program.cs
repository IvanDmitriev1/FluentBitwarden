using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using Windows.Win32;
using Windows.Win32.Foundation;
namespace FluentBitwarden;

public static class Program
{
    private static Microsoft.Win32.SafeHandles.SafeFileHandle? _redirectEventHandle;

    [STAThread]
    static int Main()
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        AppInstance keyInstance = AppInstance.FindOrRegisterForKey("FluentBitwardenUiSingleInstance");

        if (!keyInstance.IsCurrent)
        {
            RedirectActivationTo(AppInstance.GetCurrent().GetActivatedEventArgs(), keyInstance);
            return 0;
        }

        keyInstance.Activated += static (_, _) => App.Current.HandleActivation();

        Microsoft.UI.Xaml.Application.Start(_ =>
        {
            var context = new DispatcherQueueSynchronizationContext(
                DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            var app = new App();
        });

        return 0;
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

        const uint CoWaitDefault = 0;
        const uint Infinite = 0xFFFFFFFF;

        HANDLE rawHandle = new(_redirectEventHandle.DangerousGetHandle());
        PInvoke.CoWaitForMultipleObjects(CoWaitDefault, Infinite, [rawHandle], out _);
    }
}
