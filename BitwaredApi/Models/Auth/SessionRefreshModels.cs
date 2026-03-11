namespace BitwaredApi.Models.Auth;

public sealed record SessionRefreshRequest(
    PersistableSession Session,
    BitwardenDeviceInfo DeviceInfo);

public abstract record SessionRefreshOutcome
{
    private SessionRefreshOutcome()
    {
    }

    public sealed record Success(
        PersistableSession Session,
        string AccessToken) : SessionRefreshOutcome;

    public sealed record ReauthenticationRequired(string Message) : SessionRefreshOutcome;
}
