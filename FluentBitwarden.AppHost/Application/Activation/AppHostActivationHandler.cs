using FluentBitwarden.AppHost.Infrastructure.Abstractions;
using FluentBitwarden.AppHost.Application.Sessions;
using Microsoft.Windows.AppLifecycle;

namespace FluentBitwarden.AppHost.Application.Activation;

internal sealed class AppHostActivationHandler(
    IUiProcessLauncher uiProcessLauncher,
    IVaultSessionCoordinator vaultSessionCoordinator)
{
    public void Handle(AppActivationArguments args)
    {
        AppHostCliCommand command = AppHostCliCommandExtensions.From(args);
        switch (command)
        {
            case AppHostCliCommand.Start:
                uiProcessLauncher.ActivateMainWindow();
                break;
            case AppHostCliCommand.Lock:
                vaultSessionCoordinator.RequestLock();
                break;
            case AppHostCliCommand.Headless:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command), command, null);
        }
    }
}
