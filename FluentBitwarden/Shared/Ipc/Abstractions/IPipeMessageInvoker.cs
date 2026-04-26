namespace FluentBitwarden.Shared.Ipc.Abstractions;

public interface IPipeMessageInvoker
{
    ushort MessageType { get; }

    ValueTask InvokeAsync(Stream stream, int payloadLength, CancellationToken cancellationToken);
}