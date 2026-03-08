using FluentBitwarden.Models.Session;

namespace FluentBitwarden.Abstractions;

public interface ISessionStore
{
    ValueTask<SessionState?> LoadAsync(CancellationToken cancellationToken = default);
    ValueTask SaveAsync(SessionState state, CancellationToken cancellationToken = default);
    ValueTask ClearAsync(CancellationToken cancellationToken = default);
}
