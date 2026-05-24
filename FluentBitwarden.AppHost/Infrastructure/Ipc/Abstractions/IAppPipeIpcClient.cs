namespace FluentBitwarden.Infrastructure.Ipc.Abstractions;

public interface IAppPipeIpcClient
{
    ValueTask<TResponse> SendAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : IPipeRequestMessage
        where TResponse : notnull;
}
