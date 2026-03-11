using BitwaredApi.Models.Auth;
using FluentBitwarden.Models.Session;

namespace FluentBitwarden.Abstractions;

/// <summary>
/// Coordinates stored session state, runtime secrets, and token access for the active account.
/// </summary>
internal interface ISessionManager
{
    /// <summary>
    /// Loads the stored session projection for the active account, if one exists.
    /// </summary>
    ValueTask<StoredSessionInfo?> GetStoredSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a newly authenticated session and caches its runtime secrets.
    /// </summary>
    ValueTask CompleteAuthenticationAsync(
        AuthenticationSuccess authentication,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the persisted session state or fails when none is available.
    /// </summary>
    ValueTask<PersistableSession> RequirePersistedSessionAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an access token for Bitwarden API calls, refreshing it when needed.
    /// </summary>
    ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlocks the stored session with a decrypted user key.
    /// </summary>
    ValueTask<SessionUnlockOutcome> UnlockWithUserKeyAsync(
        byte[] userKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a copy of the currently unlocked user key, if one is loaded.
    /// </summary>
    byte[]? GetUnlockedUserKeyCopy();

    /// <summary>
    /// Clears runtime secrets while keeping the stored session.
    /// </summary>
    ValueTask LockAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the stored session, runtime secrets, and local unlock state.
    /// </summary>
    ValueTask LogoutAsync(CancellationToken cancellationToken = default);
}
