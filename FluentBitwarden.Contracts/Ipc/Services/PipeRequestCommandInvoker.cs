using FluentBitwarden.Contracts.Ipc.Internal;
using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.Contracts.Ipc.Services;

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
        if (payloadLength != 0)
        {
            throw new InvalidOperationException(
                $"IPC message '{MessageType}' does not accept a request payload, " +
                $"but received '{payloadLength}' bytes.");
        }

        var request = await PipeProtocol.ReadRequestPayloadAsync<TRequest>(
            stream,
            payloadLength,
            cancellationToken);

        await _handler(
            request,
            cancellationToken);

        await PipeProtocol.WriteResponseMessageAsync(
            stream,
            new IpcVoid(),
            cancellationToken);
    }
}