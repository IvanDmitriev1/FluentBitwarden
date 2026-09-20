using FluentBitwarden.AppHost.Hosting.Cli;
using FluentBitwarden.AppHost.Infrastructure.Processes;

namespace FluentBitwarden.AppHost.Hosting.Handlers;

internal sealed class AppActivationHandler(IUiProcessLauncher uiProcessLauncher)
{
    public void Handle(AppHostCliCommand command)
    {
        switch (command)
        {
            case AppHostCliCommand.Headless:
                return;

            case AppHostCliCommand.Lock:
                // Lock session here eventually.
                return;

            case AppHostCliCommand.Start:
                uiProcessLauncher.ActivateMainWindow();
                return;

            default:
                throw new ArgumentOutOfRangeException(nameof(command));
        }
    }
}
