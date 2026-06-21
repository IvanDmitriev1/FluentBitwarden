namespace FluentBitwarden.Platform.Ipc.Models;

internal enum IpcEndpointHandlerMethodKind
{
    RequestResponse,
    RequestCommand,
    CommandResponse,
    Command
}