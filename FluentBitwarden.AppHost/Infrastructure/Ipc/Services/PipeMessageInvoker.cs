using FluentBitwarden.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Infrastructure.Ipc.Internal;

namespace FluentBitwarden.Infrastructure.Ipc.Services;

[Fody.ConfigureAwait(false)]
public sealed class PipeMessageInvoker<THandler, TRequest, TResponse>(
    THandler handler)
    : IPipeMessageInvoker
    where THandler : class, IPipeRequestMessageHandler<TRequest, TResponse>
    where TRequest : IPipeRequestMessage
    where TResponse : notnull
{
    public async ValueTask InvokeAsync(Stream stream, int payloadLength, CancellationToken cancellationToken)
    {
        var request = await PipeProtocol.ReadPayloadAsync<TRequest>(stream, payloadLength, cancellationToken);
        var response = await handler.HandleAsync(request, cancellationToken);

        await PipeProtocol.WriteResponseMessageAsync(stream, response, cancellationToken);
    }
}
