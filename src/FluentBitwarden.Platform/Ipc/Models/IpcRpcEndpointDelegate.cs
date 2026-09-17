namespace FluentBitwarden.Platform.Ipc.Models;

internal delegate ValueTask IpcRpcEndpointDelegate(
    Stream stream,
    byte[] payload,
    CancellationToken cancellationToken);
