namespace FluentBitwarden.Platform.Ipc.Models;

internal enum IpcRpcHandlerMethodKind
{
    RequestResponse,
    RequestCommand,
    CommandResponse,
    Command
}
