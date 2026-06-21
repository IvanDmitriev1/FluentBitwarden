namespace FluentBitwarden.Platform.Ipc.Internal;

internal enum HandlerMethodKind
{
    RequestResponse,
    RequestCommand,
    CommandResponse,
    Command
}