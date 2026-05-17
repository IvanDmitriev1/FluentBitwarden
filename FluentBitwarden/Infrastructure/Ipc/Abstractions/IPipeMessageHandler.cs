namespace FluentBitwarden.Infrastructure.Ipc.Abstractions;

public interface IPipeMessageHandler<in TRequest, TResponse>
    where TRequest : notnull
    where TResponse : notnull
{
    ValueTask<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken);
}
