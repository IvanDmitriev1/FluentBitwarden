namespace BitwaredApi.Models.Auth;

public sealed record MasterPasswordUnlockRequest(
    PersistableSession Session,
    string MasterPassword);

public abstract record MasterPasswordUnlockOutcome
{
    private MasterPasswordUnlockOutcome()
    {
    }

    public sealed record Success(
        byte[] UserKey,
        byte[] LocalVaultProtectionKey) : MasterPasswordUnlockOutcome;

    public sealed record InvalidCredentials(string Message) : MasterPasswordUnlockOutcome;
}
