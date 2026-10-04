using FluentBitwarden.Contracts.AppSession;

namespace FluentBitwarden.Platform.Ipc.Services;

internal sealed class IpcExceptionNotifier : IIpcExceptionHandler, IIpcExceptionNotifier
{
    public event Action<UnlockedSessionRequiredException>? UnlockedSessionRequired;

    public void Handle(Exception exception)
    {
        if (exception is UnlockedSessionRequiredException unlockedSessionRequiredException)
            UnlockedSessionRequired?.Invoke(unlockedSessionRequiredException);
    }
}
