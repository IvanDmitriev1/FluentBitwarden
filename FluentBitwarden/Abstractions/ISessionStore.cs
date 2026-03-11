using BitwaredApi.Models.Auth;

namespace FluentBitwarden.Abstractions;

/// <summary>
/// Persists and retrieves the serialized session state for the app.
/// </summary>
internal interface ISessionStore
{
    /// <summary>
    /// Loads the persisted session state, if one is available.
    /// </summary>
    ValueTask<PersistableSession?> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the persisted session state.
    /// </summary>
    ValueTask SaveAsync(PersistableSession state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the persisted session state.
    /// </summary>
    ValueTask ClearAsync(CancellationToken cancellationToken = default);
}
