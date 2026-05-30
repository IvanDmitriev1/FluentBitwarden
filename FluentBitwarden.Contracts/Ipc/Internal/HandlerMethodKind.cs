namespace FluentBitwarden.Contracts.Ipc.Internal;

internal enum HandlerMethodKind
{
    RequestResponse,
    RequestCommand,
    CommandResponse,
    Command
}