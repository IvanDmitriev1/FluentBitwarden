using System.Text.Json.Serialization.Metadata;
using FluentBitwarden.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Infrastructure.Ipc.Internal;

namespace FluentBitwarden.Infrastructure.Ipc.Services;

[Fody.ConfigureAwait(false)]
public sealed class PipeMessageInvoker<TRequest, TResponse>(
    IPipeMessageHandler<TRequest, TResponse> handler,
    JsonTypeInfo<TRequest> requestTypeInfo,
    JsonTypeInfo<TResponse> responseTypeInfo)
    : IPipeMessageInvoker
    where TRequest : notnull
    where TResponse : notnull
{
    public async ValueTask InvokeAsync(Stream stream, int payloadLength, CancellationToken cancellationToken)
    {
        var request = await PipeProtocol.ReadPayloadAsync(stream, payloadLength, requestTypeInfo, cancellationToken);
        var response = await handler.HandleAsync(request, cancellationToken);

        await PipeProtocol.WriteResponseMessageAsync(stream, response, responseTypeInfo, cancellationToken);
    }
}
