using FluentBitwarden.Models.Session;
using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.Services;

internal static class VaultSessionStateFactory
{
    public static VaultSessionState Create(StoredSessionInfo? session)
        => session switch
        {
            null => new VaultSessionState.NoSession(),
            { IsLocked: true } => new VaultSessionState.Locked(session),
            _ => new VaultSessionState.Unlocked(session),
        };
}
