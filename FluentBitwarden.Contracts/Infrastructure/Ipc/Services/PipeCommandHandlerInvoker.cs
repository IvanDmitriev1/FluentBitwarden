using FluentBitwarden.Contracts.Infrastructure.Ipc.Internal;

namespace FluentBitwarden.Contracts.Infrastructure.Ipc.Services;

internal sealed class PipeCommandHandlerInvoker<THandler>(
    THandler handler,
    HandlerMethodDescriptor descriptor) : IIpcRequestHandlerInvoker
    where THandler : class, IIpcRequestsHandler
{
    private readonly IpcCommandHandlerDelegate _handler = descriptor.Method.CreateDelegate<IpcCommandHandlerDelegate>(handler);

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

        await _handler.Invoke(cancellationToken);

        await PipeProtocol.WriteResponseMessageAsync(
            stream,
            new IpcVoid(),
            cancellationToken);
    }
}