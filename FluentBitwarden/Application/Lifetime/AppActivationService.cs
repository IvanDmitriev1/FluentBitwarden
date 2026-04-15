using Microsoft.Extensions.Logging;
using Microsoft.Windows.AppLifecycle;

namespace FluentBitwarden.Application.Lifetime;

internal sealed class AppActivationService(
    IMainWindowService mainWindowService,
    ILogger<AppActivationService> logger) : IAppActivationService
{
    public Task InitializeAsync(AppActivationArguments initialActivation, CancellationToken cancellationToken = default)
    {
        mainWindowService.Show();
        return Task.CompletedTask;
    }

    public Task HandleAsync(AppActivationArguments activation, CancellationToken cancellationToken = default)
    {
        mainWindowService.Show();
        return Task.CompletedTask;
    }
}
