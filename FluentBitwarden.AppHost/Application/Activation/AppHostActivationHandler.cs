using FluentBitwarden.AppHost.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.AppLifecycle;

namespace FluentBitwarden.AppHost.Application.Activation;

internal sealed class AppHostActivationHandler(IHostApplicationLifetime applicationLifetime)
{
    public void Handle(AppActivationArguments args)
    {
        AppHostCliCommand command = AppHostCliCommandExtensions.From(args);
        if (command == AppHostCliCommand.Start)
        {
            AppProcessLauncher.Activate();
        }
    }
}
