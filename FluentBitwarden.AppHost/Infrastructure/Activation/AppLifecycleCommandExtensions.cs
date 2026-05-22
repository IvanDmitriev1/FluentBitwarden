using Windows.ApplicationModel.Activation;
using Microsoft.Windows.AppLifecycle;

namespace FluentBitwarden.AppHost.Infrastructure.Activation;

internal static class AppLifecycleCommandExtensions
{
    public static AppLifecycleCommand From(AppActivationArguments args)
    {
        if (args.Data is not ILaunchActivatedEventArgs launchArgs)
            return AppLifecycleCommand.Show;

        foreach (string argument in launchArgs.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (TryParse(argument, out AppLifecycleCommand command))
                return command;
        }

        return AppLifecycleCommand.Show;
    }

    private static bool TryParse(string argument, out AppLifecycleCommand command)
    {
        AppLifecycleCommand? parsedCommand = argument switch
        {
            "--show" => AppLifecycleCommand.Show,
            "--headless" => AppLifecycleCommand.Headless,
            "--lock" => AppLifecycleCommand.Lock,
            "--exit" => AppLifecycleCommand.Exit,
            _ => null
        };

        command = parsedCommand.GetValueOrDefault();
        return parsedCommand.HasValue;
    }
}