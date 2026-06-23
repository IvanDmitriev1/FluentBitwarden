using Windows.ApplicationModel.Activation;
using Microsoft.Windows.AppLifecycle;

namespace FluentBitwarden.Infrastructure.UiCommand;

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