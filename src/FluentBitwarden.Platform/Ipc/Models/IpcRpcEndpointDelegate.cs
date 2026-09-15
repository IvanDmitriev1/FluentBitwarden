namespace FluentBitwarden.Platform.Ipc.Models;

internal delegate ValueTask IpcRpcEndpointDelegate(
    Stream stream,
    int payloadLength,
    CancellationToken cancellationToken);
