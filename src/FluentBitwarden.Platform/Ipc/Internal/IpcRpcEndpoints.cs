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
    private readonly Func<THandler, TRequest, CancellationToken, Task<TResponse>> _handler =
        descriptor.Method.CreateDelegate<Func<THandler, TRequest, CancellationToken, Task<TResponse>>>();

    protected override async Task<byte[]> InvokeCoreAsync(
        IServiceProvider requestServices,
        Stream stream,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        var request = await IpcWireProtocol.ReadMessagePayloadAsync<TRequest>(payload, cancellationToken);
        var handler = requestServices.GetRequiredService<THandler>();
        var response = await _handler.Invoke(handler, request, cancellationToken);

        return IpcWireProtocol.SerializeRpcResponse(response);
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
    private readonly Func<THandler, TRequest, CancellationToken, Task> _handler =
        descriptor.Method.CreateDelegate<Func<THandler, TRequest, CancellationToken, Task>>();

    protected override async Task<byte[]> InvokeCoreAsync(
        IServiceProvider requestServices,
        Stream stream,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        var request = await IpcWireProtocol.ReadMessagePayloadAsync<TRequest>(payload, cancellationToken);
        var handler = requestServices.GetRequiredService<THandler>();
        await _handler.Invoke(handler, request, cancellationToken);

        return IpcWireProtocol.SerializeRpcResponse(IpcVoid.Value);
    }
}
