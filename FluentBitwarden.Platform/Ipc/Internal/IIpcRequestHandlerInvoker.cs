namespace FluentBitwarden.Platform.Ipc.Internal;

internal interface IIpcRequestHandlerInvoker
{
    ushort MessageType { get; }

    ValueTask InvokeAsync(Stream stream, int payloadLength, CancellationToken cancellationToken);
}