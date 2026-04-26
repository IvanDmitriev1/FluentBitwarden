using System.Text.Json.Serialization.Metadata;
using FluentBitwarden.Shared.Ipc.Abstractions;
using FluentBitwarden.Shared.Ipc.Internal;

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
        var request = await PipeProtocol.ReadPayloadAsync(stream, payloadLength, requestTypeInfo, cancellationToken);
        var response = await handler.HandleAsync(request, cancellationToken);

        await PipeProtocol.WriteMessageAsync(stream, MessageType, response, responseTypeInfo, cancellationToken);
    }
}