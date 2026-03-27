using System.Diagnostics;
using Windows.ApplicationModel.Core;
using Microsoft.Windows.AppLifecycle;

namespace FluentBitwarden.Application.Lifetime;

internal sealed class AppRestartService : IAppRestartService
{
    public Task RestartForLockAsync()
    {
        var reason = AppInstance.Restart("--restart=lock");
        Debug.Assert(reason == AppRestartFailureReason.RestartPending);

        return Task.CompletedTask;
    }
}