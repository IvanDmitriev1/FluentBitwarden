using FluentBitwarden.Models.Auth;
using FluentBitwarden.Models.Session;

namespace FluentBitwarden.Abstractions.UnlockServices;

public interface IMasterPasswordUnlockService
{
    ValueTask<AuthSession> UnlockAsync(
        StoredSessionInfo session,
        string masterPassword,
        CancellationToken cancellationToken = default);
}
