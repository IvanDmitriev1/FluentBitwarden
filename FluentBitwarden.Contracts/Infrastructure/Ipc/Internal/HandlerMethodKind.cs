namespace FluentBitwarden.Contracts.Infrastructure.Ipc.Internal;

internal enum HandlerMethodKind
{
    RequestResponse,
    RequestCommand,
    CommandResponse,
    Command
}