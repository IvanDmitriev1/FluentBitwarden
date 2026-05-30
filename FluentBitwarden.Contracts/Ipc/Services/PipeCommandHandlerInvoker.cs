using FluentBitwarden.Contracts.Ipc.Abstractions;
using FluentBitwarden.Contracts.Ipc.Internal;

namespace FluentBitwarden.Contracts.Ipc.Services;

internal sealed class PipeCommandHandlerInvoker<THandler>
    : IIpcRequestHandlerInvoker
    where THandler : class, IIpcRequestsHandler
{
    private readonly IpcCommandHandlerDelegate _handler;

    public PipeCommandHandlerInvoker(
        THandler handler,
        HandlerMethodDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(descriptor);

        MessageType = descriptor.MessageType;

        _handler = descriptor.Method
            .CreateDelegate<IpcCommandHandlerDelegate>(handler);
    }

    public ushort MessageType { get; }

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

        await _handler(cancellationToken);

        await PipeProtocol.WriteResponseMessageAsync(
            stream,
            new IpcVoid(),
            cancellationToken);
    }
}