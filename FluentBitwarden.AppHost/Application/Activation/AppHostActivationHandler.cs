using FluentBitwarden.AppHost.Infrastructure.Abstractions;
using Microsoft.Windows.AppLifecycle;

namespace FluentBitwarden.AppHost.Application.Activation;

internal sealed class AppHostActivationHandler(IUiProcessLauncher uiProcessLauncher)
{
    public void Handle(AppActivationArguments args)
    {
        AppHostCliCommand command = AppHostCliCommandExtensions.From(args);
        if (command == AppHostCliCommand.Start)
        {
            uiProcessLauncher.ActivateMainWindow();
        }
    }
}
