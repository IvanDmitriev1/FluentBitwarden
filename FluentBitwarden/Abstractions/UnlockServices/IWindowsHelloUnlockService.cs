using FluentBitwarden.Models.Session;
using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.Abstractions.UnlockServices;

/// <summary>
/// Manages Windows Hello enrollment and Windows Hello-based vault unlock.
/// </summary>
internal interface IWindowsHelloUnlockService
{
    /// <summary>
    /// Checks whether Windows Hello can be used on the current device.
    /// </summary>
    ValueTask<bool> CanSetupAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether Windows Hello unlock is configured for the session.
    /// </summary>
    ValueTask<bool> IsConfiguredAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enrolls Windows Hello unlock for the session.
    /// </summary>
    ValueTask<VaultConfigurationOutcome> SetupAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes Windows Hello unlock enrollment for the session.
    /// </summary>
    ValueTask<VaultConfigurationOutcome> DisableAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlocks the session by using Windows Hello.
    /// </summary>
    ValueTask<SessionUnlockOutcome> UnlockAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken = default);
}
