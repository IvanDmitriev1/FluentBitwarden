using Windows.ApplicationModel.Activation;
using Microsoft.Windows.AppLifecycle;

namespace FluentBitwarden.AppHost.Application.Activation;

internal static class AppHostCliCommandExtensions
{
    public static AppHostCliCommand From(AppActivationArguments args)
    {
        if (args.Data is not ILaunchActivatedEventArgs launchArgs)
        {
            throw new NotSupportedException("AppActivationArguments is not ILaunchActivatedEventArgs");
        }

        var arguments = launchArgs.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1);
        var firstOrDefault = arguments.FirstOrDefault();

        AppHostCliCommand parsedCommand = firstOrDefault switch
        {
            "--headless" => AppHostCliCommand.Headless,
            "--lock" => AppHostCliCommand.Lock,
            _ => AppHostCliCommand.Start
        };

        return parsedCommand;
    }
}