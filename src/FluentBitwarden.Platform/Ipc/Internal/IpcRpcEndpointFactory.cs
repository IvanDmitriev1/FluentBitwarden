using FluentBitwarden.Platform.Ipc.Models;
using FluentBitwarden.Platform.Ipc.Transport;
using System.Reflection;

namespace FluentBitwarden.Platform.Ipc.Internal;

internal static class IpcRpcEndpointFactory
{
    private static readonly MethodInfo CreateRequestResponseMethod = GetFactoryMethod(nameof(CreateRequestResponse));
    private static readonly MethodInfo CreateRequestCommandMethod = GetFactoryMethod(nameof(CreateRequestCommand));
    private static readonly MethodInfo CreateCommandResponseMethod = GetFactoryMethod(nameof(CreateCommandResponse));

    [RequiresDynamicCode("IPC endpoint creation closes generic endpoint factories at runtime.")]
    [RequiresUnreferencedCode("IPC endpoint creation reflects over handler methods.")]
    public static IpcRpcEndpoint Create<THandler>(
        THandler handler,
        IpcRpcHandlerMethodDescriptor descriptor)
        where THandler : class, IIpcRequestsHandler
    {
        return descriptor.Kind switch
        {
            IpcRpcHandlerMethodKind.RequestResponse =>
                (IpcRpcEndpoint)CreateRequestResponseMethod
                    .MakeGenericMethod(descriptor.RequestType!, descriptor.ResponseType!)
                    .Invoke(null, [handler, descriptor])!,

            IpcRpcHandlerMethodKind.RequestCommand =>
                (IpcRpcEndpoint)CreateRequestCommandMethod
                    .MakeGenericMethod(descriptor.RequestType!)
                    .Invoke(null, [handler, descriptor])!,

            IpcRpcHandlerMethodKind.CommandResponse =>
                (IpcRpcEndpoint)CreateCommandResponseMethod
                    .MakeGenericMethod(descriptor.ResponseType!)
                    .Invoke(null, [handler, descriptor])!,

            IpcRpcHandlerMethodKind.Command => CreateCommand(handler, descriptor),

            _ => throw new ArgumentOutOfRangeException(
                nameof(descriptor),
                descriptor.Kind,
                "Unsupported IPC endpoint kind.")
        };
    }

    private static IpcRpcEndpoint CreateRequestResponse<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TRequest,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResponse>(
        object handler,
        IpcRpcHandlerMethodDescriptor descriptor)
        where TRequest : IIpcRequestMessage
        where TResponse : notnull
    {
        var method = descriptor.Method.CreateDelegate<Func<TRequest, CancellationToken, ValueTask<TResponse>>>(handler);

        return CreateEndpoint<TResponse>(
            descriptor,
            async (stream, payloadLength, cancellationToken) =>
            {
                var request = await IpcWireProtocol.ReadMessagePayloadAsync<TRequest>(
                    stream,
                    payloadLength,
                    cancellationToken);

                return await method(request, cancellationToken);
            });
    }

    private static IpcRpcEndpoint CreateRequestCommand<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TRequest>(
        object handler,
        IpcRpcHandlerMethodDescriptor descriptor)
        where TRequest : IIpcRequestMessage
    {
        var method = descriptor.Method.CreateDelegate<Func<TRequest, CancellationToken, ValueTask>>(handler);

        return CreateEndpoint(
            descriptor,
            async (stream, payloadLength, cancellationToken) =>
            {
                var request = await IpcWireProtocol.ReadMessagePayloadAsync<TRequest>(
                    stream,
                    payloadLength,
                    cancellationToken);

                await method(request, cancellationToken);
                return IpcVoid.Value;
            });
    }

    private static IpcRpcEndpoint CreateCommandResponse<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResponse>(
        object handler,
        IpcRpcHandlerMethodDescriptor descriptor)
        where TResponse : notnull
    {
        var method = descriptor.Method.CreateDelegate<Func<CancellationToken, ValueTask<TResponse>>>(handler);

        return CreateEndpoint<TResponse>(
            descriptor,
            async (stream, payloadLength, cancellationToken) =>
            {
                ThrowIfCommandHasPayload(descriptor.MessageType, payloadLength);

                return await method(cancellationToken);
            });
    }

    private static IpcRpcEndpoint CreateCommand(
        object handler,
        IpcRpcHandlerMethodDescriptor descriptor)
    {
        var method = descriptor.Method
            .CreateDelegate<Func<CancellationToken, ValueTask>>(handler);

        return CreateEndpoint<IpcVoid>(
            descriptor,
            async (stream, payloadLength, cancellationToken) =>
            {
                ThrowIfCommandHasPayload(descriptor.MessageType, payloadLength);

                await method(cancellationToken);
                return IpcVoid.Value;
            });
    }

    private static IpcRpcEndpoint CreateEndpoint<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResponse>(
        IpcRpcHandlerMethodDescriptor descriptor,
        Func<Stream, int, CancellationToken, ValueTask<TResponse>> body)
        where TResponse : notnull
    {
        return new IpcRpcEndpoint(
            descriptor.MessageType,
            descriptor.AuthenticationLevel,
            Invoke);

        async ValueTask Invoke(
            Stream stream,
            int payloadLength,
            CancellationToken cancellationToken)
        {
            var response = await body.Invoke(stream, payloadLength, cancellationToken);
            await IpcWireProtocol.WriteRpcResponseAsync(
                stream,
                response,
                cancellationToken);
        }
    }

    private static void ThrowIfCommandHasPayload(ushort messageType, int payloadLength)
    {
        if (payloadLength != 0)
        {
            throw new InvalidOperationException(
                $"IPC message '{messageType}' does not accept a request payload, " +
                $"but received '{payloadLength}' bytes.");
        }
    }

    private static MethodInfo GetFactoryMethod(string name) =>
        typeof(IpcRpcEndpointFactory).GetMethod(
            name,
            BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException($"Could not find {name}.");
}
