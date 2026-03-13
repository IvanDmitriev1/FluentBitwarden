using FluentBitwarden.Models.Session;
using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.Abstractions.UnlockServices;

/// <summary>
/// Resolves unlock configuration status for a stored session.
/// </summary>
public interface ILocalUnlockStatusService
{
    ValueTask<LocalUnlockStatus> GetAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken = default);
}
