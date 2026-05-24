namespace FluentBitwarden.Infrastructure.Ipc.Abstractions;

public interface IPipeRequestMessageHandler<in TRequest, TResponse>
    where TRequest : IPipeRequestMessage
    where TResponse : notnull
{
    ValueTask<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken);
}
