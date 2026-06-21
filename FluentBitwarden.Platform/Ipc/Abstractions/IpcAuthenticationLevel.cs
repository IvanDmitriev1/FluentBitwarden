namespace FluentBitwarden.Platform.Ipc.Abstractions;

public enum IpcAuthenticationLevel : byte
{
    Anonymous = 0,
    Authenticated = 1,
}
