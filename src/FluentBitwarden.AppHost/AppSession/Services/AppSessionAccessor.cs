using FluentBitwarden.AppHost.AppSession.Contracts;

namespace FluentBitwarden.AppHost.AppSession.Services;

internal sealed class AppSessionAccessor(AppSessionStore store) : IAppSessionAccessor, IAsyncDisposable
{
    private readonly Lazy<AppSessionState> _state = new(store.AcquireState);
    private readonly Lock _lock = new();
    private bool _disposed;

    public AppSessionState State
    {
        get
        {
            lock (_lock)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return _state.Value;
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        AppSessionState state;

        lock (_lock)
        {
            if (_disposed)
                return ValueTask.CompletedTask;

            _disposed = true;
            if (!_state.IsValueCreated)
                return ValueTask.CompletedTask;

            state = _state.Value;
        }

        return state.ReleaseAsync();
    }
}
