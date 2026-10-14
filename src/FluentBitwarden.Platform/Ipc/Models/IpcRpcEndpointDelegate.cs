namespace FluentBitwarden.Platform.Ipc.Models;

internal delegate Task IpcRpcEndpointDelegate(
    Stream stream,
    byte[] payload,
    CancellationToken cancellationToken);
