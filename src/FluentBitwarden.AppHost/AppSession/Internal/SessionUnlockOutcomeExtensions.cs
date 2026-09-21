using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.Contracts.AppSession.Unlock;

namespace FluentBitwarden.AppHost.AppSession.Internal;

internal static class SessionUnlockOutcomeExtensions
{
    public static SessionUnlockOutcome ConvertFailure(AccountKeyUnlockResult result, SessionUnlockRequest request) =>
        result switch
        {
            AccountKeyUnlockResult.InvalidCredentials =>
                new SessionUnlockOutcome.Failure("Invalid credentials."),

            AccountKeyUnlockResult.Cancelled
                when request is SessionUnlockRequest.WindowsHelloRequest =>
                new SessionUnlockOutcome.WindowsHelloCancelled(),

            AccountKeyUnlockResult.RequiresOnlineReauthentication =>
                new SessionUnlockOutcome.RequiresOnlineReauth(),

            _ => new SessionUnlockOutcome.Failure("Unlock failed.")
        };
}
