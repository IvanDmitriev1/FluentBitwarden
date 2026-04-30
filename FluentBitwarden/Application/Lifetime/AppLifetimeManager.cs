using Microsoft.Windows.AppLifecycle;
using System.Diagnostics;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using WinUIEx;

namespace FluentBitwarden.Application.Lifetime;

internal static class AppLifetimeManager
{
    public static void Activate(AppActivationArguments args)
    {
        TrayIconService.EnsureCreated();
        var lunchArgs = (ILaunchActivatedEventArgs)args.Data;
        var parameters = lunchArgs.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string argument = parameters.Length > 1 ? parameters[1] : string.Empty;

        switch (argument)
        {
            case "--headless":
                break;
            default:
                WindowManager.ShowMainWindow();
                break;
        }
    }

    public static void RestartApp()
    {
        var reason = AppInstance.Restart("--headless");
        Debug.Assert(reason == AppRestartFailureReason.RestartPending);
    }
}
