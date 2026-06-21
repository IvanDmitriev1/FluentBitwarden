namespace FluentBitwarden.Platform.Ipc.Abstractions;

public enum IpcAuthenticationLevel : byte
{
    Rejected,
    PackagedExternalProxy,
    SamePackage,
}
