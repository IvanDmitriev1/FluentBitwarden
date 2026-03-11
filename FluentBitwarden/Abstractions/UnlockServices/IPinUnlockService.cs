using FluentBitwarden.Models.Session;
using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.Abstractions.UnlockServices;

/// <summary>
/// Manages PIN enrollment and PIN-based vault unlock for a stored session.
/// </summary>
internal interface IPinUnlockService
{
    /// <summary>
    /// Checks whether PIN unlock is configured for the session.
    /// </summary>
    ValueTask<bool> IsConfiguredAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enrolls PIN unlock for the session.
    /// </summary>
    ValueTask<VaultConfigurationOutcome> SetupAsync(
        StoredSessionInfo session,
        string pin,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes PIN unlock enrollment for the session.
    /// </summary>
    ValueTask<VaultConfigurationOutcome> DisableAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlocks the session by using the supplied PIN.
    /// </summary>
    ValueTask<SessionUnlockOutcome> UnlockAsync(
        StoredSessionInfo session,
        string pin,
        CancellationToken cancellationToken = default);
}
