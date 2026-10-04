using FluentBitwarden.Contracts.AppSession;

namespace FluentBitwarden.Platform.Ipc.Abstractions;

public interface IIpcExceptionNotifier
{
    event Action<UnlockedSessionRequiredException>? UnlockedSessionRequired;
}
