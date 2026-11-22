using FluentBitwarden.Contracts.Infrastructure.UiActivation;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;

namespace FluentBitwarden.Ui.Application;

internal static class UiActivationCommandParser
{
    public static UiActivationCommand ParseInitial(string[] processArguments, string? launchArguments)
    {
        var command = UiActivationCommand.Parse(processArguments);
        return command is UiActivationCommand.OpenCommand ? ParseLaunchArguments(launchArguments) : command;
    }

    public static string? GetLaunchArguments(AppActivationArguments arguments) =>
        (arguments.Data as ILaunchActivatedEventArgs)?.Arguments;

    public static UiActivationCommand ParseLaunchArguments(string? arguments) =>
        UiActivationCommand.Parse(arguments?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? []);
}
