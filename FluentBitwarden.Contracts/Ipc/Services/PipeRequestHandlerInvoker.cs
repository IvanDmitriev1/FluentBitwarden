using FluentBitwarden.Contracts.Ipc.Internal;
using FluentBitwarden.Contracts.Ipc.Transport;

namespace FluentBitwarden.Contracts.Ipc.Services;

internal sealed class PipeRequestHandlerInvoker<
    THandler,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
TRequest,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
TResponse>(
    THandler handler,
    HandlerMethodDescriptor descriptor) : IIpcRequestHandlerInvoker
    where THandler : class, IIpcRequestsHandler
    where TRequest : IIpcRequestMessage
    where TResponse : notnull
{
    private readonly IpcRequestHandlerDelegate<TRequest, TResponse> _handler = descriptor.Method
        .CreateDelegate<IpcRequestHandlerDelegate<TRequest, TResponse>>(handler);

    public ushort MessageType { get; } = TRequest.MessageType;

    public async ValueTask InvokeAsync(
        Stream stream,
        int payloadLength,
        CancellationToken cancellationToken)
    {
        var request = await PipeProtocol.ReadRequestPayloadAsync<TRequest>(
            stream,
            payloadLength,
            cancellationToken);

        var response = await _handler.Invoke(request, cancellationToken);

        await PipeProtocol.WriteResponseMessageAsync(
            stream,
            response,
            cancellationToken);
    }
}