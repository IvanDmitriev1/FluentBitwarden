using AsyncAwaitBestPractices;
using FluentBitwarden.AppHost.Modules.Sessions.Abstractions;
using FluentBitwarden.AppHost.Infrastructure.Services;
using Microsoft.Windows.AppLifecycle;

namespace FluentBitwarden.AppHost.Application.Activation;

internal sealed class AppHostActivationHandler(
    IUiProcessLauncher uiProcessLauncher,
    IVaultSessionManager sessionManager)
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
                sessionManager.LockAsync().SafeFireAndForget();
                break;
            case AppHostCliCommand.Headless:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(args), command, "Unsupported app-host CLI command.");
        }
    }
}
