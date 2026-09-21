namespace FluentBitwarden.Contracts.AppSession.Status;

public enum AppSessionStatus : byte
{
    NotAuthenticated,
    Locked,
    Unlocked
}
