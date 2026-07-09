using FluentBitwarden.AppHost.Application.Sessions;
using FluentBitwarden.AppHost.Infrastructure.Services;
using Microsoft.Windows.AppLifecycle;

namespace FluentBitwarden.AppHost.Application.Activation;

internal sealed class AppHostActivationHandler(
    IUiProcessLauncher uiProcessLauncher,
    IVaultSession vaultSession)
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
                vaultSession.RequestLock();
                break;
            case AppHostCliCommand.Headless:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command), command, null);
        }
    }
}
