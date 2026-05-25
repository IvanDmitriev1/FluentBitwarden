namespace FluentBitwarden.Infrastructure.Ipc.Abstractions;

public interface IPipeRequestHandler<in TRequest, TResponse>
    where TRequest : IPipeRequestMessage
    where TResponse : notnull
{
    ValueTask<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken);
}
