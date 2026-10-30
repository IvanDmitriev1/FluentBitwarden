using FluentBitwarden.AppHost.AppSession.Contracts;

namespace FluentBitwarden.AppHost.AppSession.Services;

internal sealed class AppSessionAccessor(AppSessionState appSessionState) : IAppSessionAccessor
{
    private bool _disposed;

    public AppSessionState AppSessionState => appSessionState;
    public CancellationToken Revoked => appSessionState.Revoked;

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, true))
            return ValueTask.CompletedTask;

        return appSessionState.ReleaseAsync();
    }
}
