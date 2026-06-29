using Windows.ApplicationModel.Activation;
using Microsoft.Windows.AppLifecycle;

namespace FluentBitwarden.Infrastructure.UiCommand;

internal static class UiActivationCommandParser
{
    public static UiCliCommand From(AppActivationArguments activation)
    {
        if (activation.Data is not ILaunchActivatedEventArgs launchArgs)
            return new UiCliCommand.OpenCommand();

        var args = launchArgs.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (args.Length <= 1)
            return new UiCliCommand.OpenCommand();

        return args[1] switch
        {
            "--exit" => new UiCliCommand.ExitCommand(),
            "--overlay" => new UiCliCommand.OverlayCommand(),
            "--open-item" => args.Length == 3
                ? ParseOpenItemCommand(args[2])
                : throw new ArgumentException("--open-item requires an <itemId>"),
            _ => throw new ArgumentException()
        };
    }

    private static UiCliCommand.OpenItemCommand ParseOpenItemCommand(ReadOnlySpan<char> data)
    {
        var cipherId = CipherId.Parse(data);
        return new UiCliCommand.OpenItemCommand(cipherId);
    }
}