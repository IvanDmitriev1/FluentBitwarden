using FluentBitwarden.Models.Session;

namespace FluentBitwarden.Abstractions.UnlockServices;

/// <summary>
/// Unlocks the stored vault session by using the account master password.
/// </summary>
internal interface IMasterPasswordUnlockService
{
    /// <summary>
    /// Unlocks the session and local vault state with the supplied master password.
    /// </summary>
    ValueTask<SessionUnlockOutcome> UnlockAsync(
        StoredSessionInfo session,
        string masterPassword,
        CancellationToken cancellationToken = default);
}
