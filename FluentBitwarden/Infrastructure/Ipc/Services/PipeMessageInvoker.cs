using FluentBitwarden.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Infrastructure.Ipc.Internal;

namespace FluentBitwarden.Infrastructure.Ipc.Services;

[Fody.ConfigureAwait(false)]
public sealed class PipeMessageInvoker<TRequest, TResponse>(
    IPipeMessageHandler<TRequest, TResponse> handler)
    : IPipeMessageInvoker
    where TRequest : IPipeMessage<TRequest>
    where TResponse : IPipeMessage<TResponse>
{
    public async ValueTask InvokeAsync(Stream stream, int payloadLength, CancellationToken cancellationToken)
    {
        var request = await PipeProtocol.ReadPayloadAsync<TRequest>(stream, payloadLength, cancellationToken);
        var response = await handler.HandleAsync(request, cancellationToken);

        await PipeProtocol.WriteResponseMessageAsync(stream, response, cancellationToken);
    }
}
