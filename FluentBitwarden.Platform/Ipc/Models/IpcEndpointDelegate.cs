namespace FluentBitwarden.Platform.Ipc.Models;

internal delegate ValueTask IpcEndpointDelegate(
    Stream stream,
    int payloadLength,
    CancellationToken cancellationToken);
