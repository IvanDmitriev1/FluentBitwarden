namespace FluentBitwarden.Platform.Ipc.Transport;

internal enum IpcRpcFailureCode : byte
{
    Generic,
    LockedSession,
    Cancellation,
}
