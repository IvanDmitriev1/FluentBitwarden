namespace FluentBitwarden.Platform.Ipc.Transport;

internal sealed class IpcRemoteException()
    : Exception("The remote IPC request failed.");
