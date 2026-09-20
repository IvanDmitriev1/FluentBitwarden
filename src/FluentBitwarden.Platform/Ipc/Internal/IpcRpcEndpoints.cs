using FluentBitwarden.Platform.Ipc.Models;
using FluentBitwarden.Platform.Ipc.Transport;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Platform.Ipc.Internal;

internal sealed class RequestResponseEndpoint<
    THandler,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TRequest,
    TResponse>(
    IpcRpcHandlerMethodDescriptor descriptor)
    : IpcRpcEndpoint(descriptor)
    where THandler : class, IIpcRequestsHandler
    where TRequest : IIpcRequestMessage
    where TResponse : notnull
{
    private readonly Func<THandler, TRequest, CancellationToken, ValueTask<TResponse>> _handler =
        descriptor.Method.CreateDelegate<Func<THandler, TRequest, CancellationToken, ValueTask<TResponse>>>();

    protected override async ValueTask InvokeCoreAsync(
        IServiceProvider requestServices,
        Stream stream,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        var request = await IpcWireProtocol.ReadMessagePayloadAsync<TRequest>(payload, cancellationToken);
        var handler = requestServices.GetRequiredService<THandler>();
        var response = await _handler.Invoke(handler, request, cancellationToken);

        await IpcWireProtocol.WriteRpcResponseAsync(stream, response, cancellationToken);
    }
}

internal sealed class RequestCommandEndpoint<
    THandler,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TRequest>(
    IpcRpcHandlerMethodDescriptor descriptor)
    : IpcRpcEndpoint(descriptor)
    where THandler : class, IIpcRequestsHandler
    where TRequest : IIpcRequestMessage
{
    private readonly Func<THandler, TRequest, CancellationToken, ValueTask> _handler =
        descriptor.Method.CreateDelegate<Func<THandler, TRequest, CancellationToken, ValueTask>>();

    protected override async ValueTask InvokeCoreAsync(
        IServiceProvider requestServices,
        Stream stream,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        var request = await IpcWireProtocol.ReadMessagePayloadAsync<TRequest>(payload, cancellationToken);
        var handler = requestServices.GetRequiredService<THandler>();
        await _handler.Invoke(handler, request, cancellationToken);

        await IpcWireProtocol.WriteRpcResponseAsync(stream, IpcVoid.Value, cancellationToken);
    }
}

internal sealed class CommandResponseEndpoint<THandler, TResponse>(
    IpcRpcHandlerMethodDescriptor descriptor)
    : IpcRpcEndpoint(descriptor)
    where THandler : class, IIpcRequestsHandler
    where TResponse : notnull
{
    private readonly Func<THandler, CancellationToken, ValueTask<TResponse>> _handler =
        descriptor.Method.CreateDelegate<Func<THandler, CancellationToken, ValueTask<TResponse>>>();

    protected override async ValueTask InvokeCoreAsync(
        IServiceProvider requestServices,
        Stream stream,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        IpcWireProtocol.ThrowIfCommandHasPayload(MessageType, payload.Length);

        var handler = requestServices.GetRequiredService<THandler>();
        var response = await _handler(handler, cancellationToken);
        await IpcWireProtocol.WriteRpcResponseAsync(stream, response, cancellationToken);
    }
}

internal sealed class CommandEndpoint<THandler>(
    IpcRpcHandlerMethodDescriptor descriptor)
    : IpcRpcEndpoint(descriptor)
    where THandler : class, IIpcRequestsHandler
{
    private readonly Func<THandler, CancellationToken, ValueTask> _handler =
        descriptor.Method.CreateDelegate<Func<THandler, CancellationToken, ValueTask>>();

    protected override async ValueTask InvokeCoreAsync(
        IServiceProvider requestServices,
        Stream stream,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        IpcWireProtocol.ThrowIfCommandHasPayload(MessageType, payload.Length);

        var handler = requestServices.GetRequiredService<THandler>();
        await _handler(handler, cancellationToken);
        await IpcWireProtocol.WriteRpcResponseAsync(stream, IpcVoid.Value, cancellationToken);
    }
}
