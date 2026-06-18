using FluentBitwarden.Contracts.Ipc.Internal;
using FluentBitwarden.Contracts.Ipc.Transport;

namespace FluentBitwarden.Contracts.Ipc.Services;

internal sealed class PipeCommandHandlerInvoker<
    THandler,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
TResponse>(
    THandler handler,
    HandlerMethodDescriptor descriptor) : IIpcRequestHandlerInvoker
    where THandler : class, IIpcRequestsHandler
    where TResponse : notnull
{
    private readonly IpcCommandHandlerDelegate<TResponse> _handler = descriptor.Method
        .CreateDelegate<IpcCommandHandlerDelegate<TResponse>>(handler);

    public ushort MessageType { get; } = descriptor.MessageType;

    public async ValueTask InvokeAsync(
        Stream stream,
        int payloadLength,
        CancellationToken cancellationToken)
    {
        if (payloadLength != 0)
        {
            throw new InvalidOperationException(
                $"IPC message '{MessageType}' does not accept a request payload, " +
                $"but received '{payloadLength}' bytes.");
        }

        var response = await _handler.Invoke(cancellationToken);
        await PipeProtocol.WriteResponseMessageAsync(
            stream,
            response,
            cancellationToken);
    }
}