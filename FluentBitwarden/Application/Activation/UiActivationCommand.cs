using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;

namespace FluentBitwarden.Application.Activation;

internal enum UiActivationCommand
{
    ShowMainWindow,
    ShowOverlay,
    Exit
}

internal static class UiActivationCommandParser
{
    public static UiActivationCommand From(AppActivationArguments args)
    {
        if (args.Data is not ILaunchActivatedEventArgs launchArgs)
            return UiActivationCommand.ShowMainWindow;

        string? firstSwitch = launchArgs.Arguments
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(static value => value.StartsWith("--", StringComparison.Ordinal));

        return firstSwitch switch
        {
            "--overlay" => UiActivationCommand.ShowOverlay,
            "--exit" => UiActivationCommand.Exit,
            _ => UiActivationCommand.ShowMainWindow
        };
    }
}
