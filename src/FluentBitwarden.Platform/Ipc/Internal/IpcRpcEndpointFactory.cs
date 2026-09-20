using FluentBitwarden.Platform.Ipc.Models;

namespace FluentBitwarden.Platform.Ipc.Internal;

internal static class IpcRpcEndpointFactory
{
    [RequiresDynamicCode(
        "IPC endpoint creation closes generic endpoint types at runtime.")]
    [RequiresUnreferencedCode(
        "IPC endpoint creation reflects over handler methods.")]
    public static IpcRpcEndpoint Create<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
        THandler>(IpcRpcHandlerMethodDescriptor descriptor)
        where THandler : class, IIpcRequestsHandler
    {
        var endpointType = descriptor.Kind switch
        {
            IpcRpcHandlerMethodKind.RequestResponse =>
                typeof(RequestResponseEndpoint<,,>).MakeGenericType(
                    typeof(THandler),
                    descriptor.RequestType!,
                    descriptor.ResponseType!),

            IpcRpcHandlerMethodKind.RequestCommand =>
                typeof(RequestCommandEndpoint<,>).MakeGenericType(
                    typeof(THandler),
                    descriptor.RequestType!),

            IpcRpcHandlerMethodKind.CommandResponse =>
                typeof(CommandResponseEndpoint<,>).MakeGenericType(
                    typeof(THandler),
                    descriptor.ResponseType!),

            IpcRpcHandlerMethodKind.Command =>
                typeof(CommandEndpoint<>).MakeGenericType(typeof(THandler)),

            _ => throw new ArgumentOutOfRangeException(
                nameof(descriptor),
                descriptor.Kind,
                "Unsupported IPC endpoint kind.")
        };

        return (IpcRpcEndpoint)Activator.CreateInstance(endpointType, descriptor)!;
    }
}
