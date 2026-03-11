using BitwaredApi.Models.Auth;

namespace FluentBitwarden.Models.Session;

internal abstract record SessionUnlockOutcome
{
    private SessionUnlockOutcome()
    {
    }

    public sealed record Success(AuthSession Session) : SessionUnlockOutcome;

    public sealed record InvalidCredentials(string Message) : SessionUnlockOutcome;

    public sealed record Unavailable(string Message) : SessionUnlockOutcome;

    public sealed record Cancelled(string Message) : SessionUnlockOutcome;
}
