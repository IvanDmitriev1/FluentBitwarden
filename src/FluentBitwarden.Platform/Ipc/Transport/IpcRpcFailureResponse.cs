namespace FluentBitwarden.Platform.Ipc.Transport;

[MemoryPackable(SerializeLayout.Explicit)]
internal readonly partial record struct IpcRpcFailureResponse(
    [property: MemoryPackOrder(0)] IpcRpcFailureCode Code);

