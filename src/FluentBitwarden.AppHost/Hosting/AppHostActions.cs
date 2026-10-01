using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Infrastructure.Processes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FluentBitwarden.AppHost.Hosting;

internal sealed class AppHostActions(
    IUiProcessLauncher uiProcessLauncher,
    IHostApplicationLifetime applicationLifetime,
    IServiceScopeFactory scopeFactory,
    ILogger<AppHostActions> logger)
{
    public void ShowMainWindow() => uiProcessLauncher.ActivateMainWindow();

    public void RequestExit() => applicationLifetime.StopApplication();

    public async void Lock()
    {
        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            IAppSessionService session = scope.ServiceProvider.GetRequiredService<IAppSessionService>();
            await session.LockAsync(CancellationToken.None);
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "AppHostActions.Lock failed");
        }
    }
}
