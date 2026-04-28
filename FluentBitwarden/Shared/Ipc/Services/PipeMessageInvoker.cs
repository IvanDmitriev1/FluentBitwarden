using FluentBitwarden.Shared.Ipc.Abstractions;
using FluentBitwarden.Shared.Ipc.Internal;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace FluentBitwarden.Shared.Ipc.Services;

[Fody.ConfigureAwait(false)]
public sealed class PipeMessageInvoker<TRequest, TResponse>(
    IPipeMessageHandler<TRequest, TResponse> handler,
    JsonTypeInfo<TRequest> requestTypeInfo,
    JsonTypeInfo<TResponse> responseTypeInfo)
    : IPipeMessageInvoker
    where TRequest : notnull
    where TResponse : notnull
{
    public ushort MessageType => handler.MessageType;

    public async ValueTask InvokeAsync(Stream stream, int payloadLength, CancellationToken cancellationToken)
    {
        IpcResult<TResponse> response;

        try
        {
            var request = await PipeProtocol.ReadPayloadAsync(stream, payloadLength, requestTypeInfo, cancellationToken);
            response = await handler.HandleAsync(request, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException and not EndOfStreamException and not IOException)
        {
            response = IpcResult<TResponse>.Fail(e.Message);
        }

        await PipeProtocol.WriteResultMessageAsync(stream, MessageType, response, responseTypeInfo, cancellationToken);
    }
}
