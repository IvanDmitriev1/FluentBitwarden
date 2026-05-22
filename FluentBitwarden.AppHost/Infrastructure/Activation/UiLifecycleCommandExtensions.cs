using Windows.ApplicationModel.Activation;
using Microsoft.Windows.AppLifecycle;

namespace FluentBitwarden.AppHost.Infrastructure.Activation;

internal static class UiLifecycleCommandExtensions
{
    public static UiLifecycleCommand From(AppActivationArguments args)
    {
        if (args.Data is not ILaunchActivatedEventArgs launchArgs)
            return UiLifecycleCommand.Show;

        foreach (string argument in launchArgs.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (TryParse(argument, out UiLifecycleCommand command))
                return command;
        }

        return UiLifecycleCommand.Show;
    }

    private static bool TryParse(string argument, out UiLifecycleCommand command)
    {
        UiLifecycleCommand? parsedCommand = argument switch
        {
            "--show" => UiLifecycleCommand.Show,
            "--headless" => UiLifecycleCommand.Headless,
            "--lock" => UiLifecycleCommand.Lock,
            "--exit" => UiLifecycleCommand.Exit,
            _ => null
        };

        command = parsedCommand.GetValueOrDefault();
        return parsedCommand.HasValue;
    }
}