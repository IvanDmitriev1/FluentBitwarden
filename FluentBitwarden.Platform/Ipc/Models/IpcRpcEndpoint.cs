namespace FluentBitwarden.Platform.Ipc.Models;

internal sealed record IpcRpcEndpoint(
    ushort MessageType,
    IpcAuthenticationLevel AuthenticationLevel,
    IpcRpcEndpointDelegate Delegate);
