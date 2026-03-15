using BitwaredApi.Models.Auth;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Extensions;
using FluentBitwarden.Services.Storage;

namespace FluentBitwarden.Services;

internal sealed class SessionStore(IAppPaths paths) : ISessionStore
{
    private readonly ProtectedJsonFileStore<PersistableSession> _store = new(
        paths.SessionFilePath,
        FluentBitwardenJsonContext.Default.PersistableSession);

    public ValueTask<PersistableSession?> LoadAsync(CancellationToken cancellationToken = default)
        => _store.LoadAsync(cancellationToken);

    public ValueTask SaveAsync(PersistableSession state, CancellationToken cancellationToken = default)
        => _store.SaveAsync(state, cancellationToken);

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
        => _store.ClearAsync(cancellationToken);
}
