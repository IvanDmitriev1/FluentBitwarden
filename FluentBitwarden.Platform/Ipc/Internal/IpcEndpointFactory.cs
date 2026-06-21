using FluentBitwarden.Platform.Ipc.Models;
using FluentBitwarden.Platform.Ipc.Transport;
using System.Reflection;

namespace FluentBitwarden.Platform.Ipc.Internal;

internal static class IpcEndpointFactory
{
    private static readonly MethodInfo CreateRequestResponseMethod = GetFactoryMethod(nameof(CreateRequestResponse));
    private static readonly MethodInfo CreateRequestCommandMethod = GetFactoryMethod(nameof(CreateRequestCommand));
    private static readonly MethodInfo CreateCommandResponseMethod = GetFactoryMethod(nameof(CreateCommandResponse));

    [RequiresDynamicCode("IPC endpoint creation closes generic endpoint factories at runtime.")]
    [RequiresUnreferencedCode("IPC endpoint creation reflects over handler methods.")]
    public static IpcEndpoint Create<THandler>(
        THandler handler,
        IpcEndpointHandlerMethodDescriptor descriptor)
        where THandler : class, IIpcRequestsHandler
    {
        return descriptor.Kind switch
        {
            IpcEndpointHandlerMethodKind.RequestResponse =>
                (IpcEndpoint)CreateRequestResponseMethod
                    .MakeGenericMethod(descriptor.RequestType!, descriptor.ResponseType!)
                    .Invoke(null, [handler, descriptor])!,

            IpcEndpointHandlerMethodKind.RequestCommand =>
                (IpcEndpoint)CreateRequestCommandMethod
                    .MakeGenericMethod(descriptor.RequestType!)
                    .Invoke(null, [handler, descriptor])!,

            IpcEndpointHandlerMethodKind.CommandResponse =>
                (IpcEndpoint)CreateCommandResponseMethod
                    .MakeGenericMethod(descriptor.ResponseType!)
                    .Invoke(null, [handler, descriptor])!,

            IpcEndpointHandlerMethodKind.Command => CreateCommand(handler, descriptor),

            _ => throw new ArgumentOutOfRangeException(
                nameof(descriptor),
                descriptor.Kind,
                "Unsupported IPC endpoint kind.")
        };
    }

    private static IpcEndpoint CreateRequestResponse<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TRequest,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResponse>(
        object handler,
        IpcEndpointHandlerMethodDescriptor descriptor)
        where TRequest : IIpcRequestMessage
        where TResponse : notnull
    {
        var method = descriptor.Method.CreateDelegate<Func<TRequest, CancellationToken, ValueTask<TResponse>>>(handler);

        return CreateEndpoint<TResponse>(
            descriptor,
            async (stream, payloadLength, cancellationToken) =>
            {
                var request = await PipeProtocol.ReadRequestPayloadAsync<TRequest>(
                    stream,
                    payloadLength,
                    cancellationToken);

                return await method(request, cancellationToken);
            });
    }

    private static IpcEndpoint CreateRequestCommand<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TRequest>(
        object handler,
        IpcEndpointHandlerMethodDescriptor descriptor)
        where TRequest : IIpcRequestMessage
    {
        var method = descriptor.Method.CreateDelegate<Func<TRequest, CancellationToken, ValueTask>>(handler);

        return CreateEndpoint(
            descriptor,
            async (stream, payloadLength, cancellationToken) =>
            {
                var request = await PipeProtocol.ReadRequestPayloadAsync<TRequest>(
                    stream,
                    payloadLength,
                    cancellationToken);

                await method(request, cancellationToken);
                return IpcVoid.Value;
            });
    }

    private static IpcEndpoint CreateCommandResponse<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResponse>(
        object handler,
        IpcEndpointHandlerMethodDescriptor descriptor)
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

    private static IpcEndpoint CreateCommand(
        object handler,
        IpcEndpointHandlerMethodDescriptor descriptor)
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

    private static IpcEndpoint CreateEndpoint<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResponse>(
        IpcEndpointHandlerMethodDescriptor descriptor,
        Func<Stream, int, CancellationToken, ValueTask<TResponse>> body)
        where TResponse : notnull
    {
        return new IpcEndpoint(
            descriptor.MessageType,
            descriptor.AuthenticationLevel,
            Invoke);

        async ValueTask Invoke(
            Stream stream,
            int payloadLength,
            CancellationToken cancellationToken)
        {
            var response = await body.Invoke(stream, payloadLength, cancellationToken);
            await PipeProtocol.WriteResponseMessageAsync(
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
        typeof(IpcEndpointFactory).GetMethod(
            name,
            BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException($"Could not find {name}.");
}
