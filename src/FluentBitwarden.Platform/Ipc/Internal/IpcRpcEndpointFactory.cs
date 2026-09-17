using FluentBitwarden.Platform.Ipc.Models;

namespace FluentBitwarden.Platform.Ipc.Internal;

internal static class IpcRpcEndpointFactory
{
    [RequiresDynamicCode(
        "IPC endpoint creation closes generic endpoint types at runtime.")]
    [RequiresUnreferencedCode(
        "IPC endpoint creation reflects over handler methods.")]
    public static IpcRpcEndpoint Create<THandler>(THandler handler, IpcRpcHandlerMethodDescriptor descriptor)
        where THandler : class, IIpcRequestsHandler
    {
        var endpointType = descriptor.Kind switch
        {
            IpcRpcHandlerMethodKind.RequestResponse =>
                typeof(RequestResponseEndpoint<,>).MakeGenericType(
                    descriptor.RequestType!,
                    descriptor.ResponseType!),

            IpcRpcHandlerMethodKind.RequestCommand =>
                typeof(RequestCommandEndpoint<>).MakeGenericType(
                    descriptor.RequestType!),

            IpcRpcHandlerMethodKind.CommandResponse =>
                typeof(CommandResponseEndpoint<>).MakeGenericType(
                    descriptor.ResponseType!),

            IpcRpcHandlerMethodKind.Command =>
                null,

            _ => throw new ArgumentOutOfRangeException(
                nameof(descriptor),
                descriptor.Kind,
                "Unsupported IPC endpoint kind.")
        };

        if (endpointType is null)
        {
            return new CommandEndpoint(handler, descriptor);
        }

        return (IpcRpcEndpoint)Activator.CreateInstance(endpointType, handler, descriptor)!;
    }
}
