using Windows.ApplicationModel.Activation;
using Microsoft.Windows.AppLifecycle;

namespace FluentBitwarden.AppHost.Hosting.Cli;

internal static class AppHostCliCommandFactory
{
    public static AppHostCliCommand ToCliCommand(this AppActivationArguments args)
    {
        if (args.Data is not ILaunchActivatedEventArgs launchArgs)
        {
            throw new NotSupportedException("AppActivationArguments is not ILaunchActivatedEventArgs");
        }

        var arguments = launchArgs.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1);
        var firstOrDefault = arguments.FirstOrDefault();

        AppHostCliCommand parsedCommand = firstOrDefault switch
        {
            "--headless" => new AppHostCliCommand.Headless(),
            "--lock" => new AppHostCliCommand.Lock(),
            _ => new AppHostCliCommand.Start()
        };

        return parsedCommand;
    }
}
