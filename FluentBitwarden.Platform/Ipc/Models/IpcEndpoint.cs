namespace FluentBitwarden.Platform.Ipc.Models;

internal sealed record IpcEndpoint(
    ushort MessageType,
    IpcAuthenticationLevel AuthenticationLevel,
    IpcEndpointDelegate Delegate);