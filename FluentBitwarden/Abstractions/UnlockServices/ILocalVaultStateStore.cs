using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.Abstractions.UnlockServices;

/// <summary>
/// Persists and retrieves local unlock state for a stored account.
/// </summary>
internal interface ILocalVaultStateStore
{
    /// <summary>
    /// Loads local unlock state for an account, if it exists.
    /// </summary>
    ValueTask<LocalVaultState?> GetForAccountAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads local unlock state for an account or fails if it is missing.
    /// </summary>
    ValueTask<LocalVaultState> RequireForAccountAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves local unlock state.
    /// </summary>
    ValueTask SaveAsync(
        LocalVaultState state,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all persisted local unlock state.
    /// </summary>
    ValueTask ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether Windows Hello unlock is enrolled for an account.
    /// </summary>
    ValueTask<bool> HasWindowsHelloEnrollmentAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether PIN unlock is enrolled for an account.
    /// </summary>
    ValueTask<bool> HasPinEnrollmentAsync(
        string accountId,
        CancellationToken cancellationToken = default);
}
