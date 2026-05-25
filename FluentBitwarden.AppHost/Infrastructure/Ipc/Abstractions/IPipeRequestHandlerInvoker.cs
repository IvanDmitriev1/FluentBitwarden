namespace FluentBitwarden.Infrastructure.Ipc.Abstractions;

public interface IPipeRequestHandlerInvoker
{
    ushort MessageType { get; }

    ValueTask InvokeAsync(Stream stream, int payloadLength, CancellationToken cancellationToken);
}