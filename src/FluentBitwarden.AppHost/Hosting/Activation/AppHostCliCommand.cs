using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;

namespace FluentBitwarden.AppHost.Hosting.Activation;

internal abstract record AppHostCliCommand
{
    private AppHostCliCommand() { }

    public sealed record Start : AppHostCliCommand;
    public sealed record Headless : AppHostCliCommand;
    public sealed record Lock : AppHostCliCommand;

    public static AppHostCliCommand Create(AppActivationArguments args)
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
