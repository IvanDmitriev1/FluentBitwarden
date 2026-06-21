using FluentBitwarden.Platform.Ipc.Internal;
using FluentBitwarden.Platform.Ipc.Transport;

namespace FluentBitwarden.Platform.Ipc.Services;

internal sealed class PipeRequestCommandInvoker<
    THandler,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
TRequest>(
    THandler handler,
    HandlerMethodDescriptor descriptor) : IIpcRequestHandlerInvoker
    where THandler : class, IIpcRequestsHandler
    where TRequest : IIpcRequestMessage
{
    private readonly IpcRequestHandlerDelegate<TRequest> _handler = descriptor.Method
        .CreateDelegate<IpcRequestHandlerDelegate<TRequest>>(handler);

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

        await _handler.Invoke(request, cancellationToken);

        await PipeProtocol.WriteResponseMessageAsync(
            stream,
            new IpcVoid(),
            cancellationToken);
    }
}