using FluentBitwarden.Platform.Ipc.Models;
using FluentBitwarden.Platform.Ipc.Transport;

namespace FluentBitwarden.Platform.Ipc.Internal;

internal sealed class RequestResponseEndpoint<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TRequest,
    TResponse>(
    object handler,
    IpcRpcHandlerMethodDescriptor descriptor)
    : IpcRpcEndpoint(descriptor)
    where TRequest : IIpcRequestMessage
    where TResponse : notnull
{
    private readonly Func<TRequest, CancellationToken, ValueTask<TResponse>> _handler =
        descriptor.Method.CreateDelegate<Func<TRequest, CancellationToken, ValueTask<TResponse>>>(handler);

    protected override async ValueTask InvokeCoreAsync(
        Stream stream,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        var request = await IpcWireProtocol.ReadMessagePayloadAsync<TRequest>(payload, cancellationToken);
        var response = await _handler.Invoke(request, cancellationToken);

        await IpcWireProtocol.WriteRpcResponseAsync(stream, response, cancellationToken);
    }
}

internal sealed class RequestCommandEndpoint<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TRequest>(
    object handler,
    IpcRpcHandlerMethodDescriptor descriptor)
    : IpcRpcEndpoint(descriptor)
    where TRequest : IIpcRequestMessage
{
    private readonly Func<TRequest, CancellationToken, ValueTask> _handler =
        descriptor.Method.CreateDelegate<Func<TRequest, CancellationToken, ValueTask>>(handler);

    protected override async ValueTask InvokeCoreAsync(
        Stream stream,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        var request = await IpcWireProtocol.ReadMessagePayloadAsync<TRequest>(payload, cancellationToken);
        await _handler.Invoke(request, cancellationToken);

        await IpcWireProtocol.WriteRpcResponseAsync(stream, IpcVoid.Value, cancellationToken);
    }
}

internal sealed class CommandResponseEndpoint<TResponse>(
    object handler,
    IpcRpcHandlerMethodDescriptor descriptor)
    : IpcRpcEndpoint(descriptor)
    where TResponse : notnull
{
    private readonly Func<CancellationToken, ValueTask<TResponse>> _handler =
        descriptor.Method.CreateDelegate<Func<CancellationToken, ValueTask<TResponse>>>(handler);

    protected override async ValueTask InvokeCoreAsync(
        Stream stream,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        IpcWireProtocol.ThrowIfCommandHasPayload(MessageType, payload.Length);

        var response = await _handler(cancellationToken);
        await IpcWireProtocol.WriteRpcResponseAsync(stream, response, cancellationToken);
    }
}

internal sealed class CommandEndpoint(
    object handler,
    IpcRpcHandlerMethodDescriptor descriptor)
    : IpcRpcEndpoint(descriptor)
{
    private readonly Func<CancellationToken, ValueTask> _handler =
        descriptor.Method.CreateDelegate<Func<CancellationToken, ValueTask>>(handler);

    protected override async ValueTask InvokeCoreAsync(
        Stream stream,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        IpcWireProtocol.ThrowIfCommandHasPayload(MessageType, payload.Length);

        await _handler(cancellationToken);
        await IpcWireProtocol.WriteRpcResponseAsync(stream, IpcVoid.Value, cancellationToken);
    }
}
