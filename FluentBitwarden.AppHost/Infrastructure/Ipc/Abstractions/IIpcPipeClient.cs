namespace FluentBitwarden.Infrastructure.Ipc.Abstractions;

public interface IIpcPipeClient
{
    ValueTask<TResponse> SendAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : IPipeRequestMessage
        where TResponse : notnull;
}
