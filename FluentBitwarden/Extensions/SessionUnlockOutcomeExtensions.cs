using FluentBitwarden.Models.Session;
using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.Extensions;

internal static class SessionUnlockOutcomeExtensions
{
    public static VaultUnlockOutcome ToVaultUnlockOutcome(this SessionUnlockOutcome outcome)
        => outcome switch
        {
            SessionUnlockOutcome.Success => new VaultUnlockOutcome.Success(),
            SessionUnlockOutcome.InvalidCredentials invalidCredentials => new VaultUnlockOutcome.InvalidCredentials(invalidCredentials.Message),
            SessionUnlockOutcome.Unavailable unavailable => new VaultUnlockOutcome.Unavailable(unavailable.Message),
            SessionUnlockOutcome.Cancelled cancelled => new VaultUnlockOutcome.Cancelled(cancelled.Message),
            _ => throw new InvalidOperationException("Unsupported session unlock outcome."),
        };
}
