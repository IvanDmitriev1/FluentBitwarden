using FluentBitwarden.AppHost.Hosting.Tray;
using FluentBitwarden.AppHost.Infrastructure.Processes;
using Microsoft.Extensions.Hosting;

namespace FluentBitwarden.AppHost.Hosting.Handlers;

internal sealed class AppTrayCommandsHandler(
    IUiProcessLauncher uiProcessLauncher,
    IHostApplicationLifetime applicationLifetime) : ITrayCommandsHandler
{
    public void HandleLeftClick()
    {

    }

    public void HandleRightClick(TrayMenuCommand command)
    {
        switch (command)
        {
            case TrayMenuCommand.Show:
                uiProcessLauncher.ActivateMainWindow();
                return;

            case TrayMenuCommand.Lock:
                // Session locking eventually.
                return;

            case TrayMenuCommand.Exit:
                applicationLifetime.StopApplication();
                return;

            default:
                throw new ArgumentOutOfRangeException(nameof(command));
        }
    }
}
