using FluentBitwarden.Models.Auth;
using FluentBitwarden.Models.Session;

namespace FluentBitwarden.Abstractions.UnlockServices;

public interface IWindowsHelloUnlockService
{
    ValueTask<bool> CanSetupAsync(
        CancellationToken cancellationToken = default);

    ValueTask<bool> IsConfiguredAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken = default);

    ValueTask SetupAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken = default);

    ValueTask DisableAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken = default);

    ValueTask<AuthSession> UnlockAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken = default);
}
