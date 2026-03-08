namespace BitwaredApi.Models.Auth;

public sealed record MasterPasswordAuth(
    byte[] MasterKey,
    byte[] StretchedMasterKey,
    string ServerAuthorizationHash);
