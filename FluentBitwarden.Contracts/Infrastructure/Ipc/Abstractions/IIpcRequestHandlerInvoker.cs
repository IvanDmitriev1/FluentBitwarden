namespace FluentBitwarden.Contracts.Infrastructure.Ipc.Abstractions;

public interface IIpcRequestHandlerInvoker
{
    ushort MessageType { get; }

    ValueTask InvokeAsync(Stream stream, int payloadLength, CancellationToken cancellationToken);
}