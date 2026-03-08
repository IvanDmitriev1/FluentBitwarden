using FluentBitwarden.Models.Auth;
using FluentBitwarden.Models.Session;

namespace FluentBitwarden.Abstractions.UnlockServices;

public interface IPinUnlockService
{
    ValueTask<bool> IsConfiguredAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken = default);

    ValueTask SetupAsync(
        StoredSessionInfo session,
        string pin,
        CancellationToken cancellationToken = default);

    ValueTask DisableAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken = default);

    ValueTask<AuthSession> UnlockAsync(
        StoredSessionInfo session,
        string pin,
        CancellationToken cancellationToken = default);
}
