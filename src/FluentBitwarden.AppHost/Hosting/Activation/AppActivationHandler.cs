using Microsoft.Windows.AppLifecycle;

namespace FluentBitwarden.AppHost.Hosting.Activation;

internal sealed class AppActivationHandler(AppHostActions actions)
{
    public void HandleActivation(AppActivationArguments arguments)
    {
        Handle(AppHostCliCommand.Create(arguments));
    }

    public void Handle(AppHostCliCommand command)
    {
        switch (command)
        {
            case AppHostCliCommand.Headless:
                return;

            case AppHostCliCommand.Lock:
                actions.Lock();
                return;

            case AppHostCliCommand.Start:
                actions.ShowMainWindow();
                return;

            default:
                throw new ArgumentOutOfRangeException(nameof(command));
        }
    }
}
