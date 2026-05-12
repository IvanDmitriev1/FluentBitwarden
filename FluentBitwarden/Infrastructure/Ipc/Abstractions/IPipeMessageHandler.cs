namespace FluentBitwarden.Infrastructure.Ipc.Abstractions;

public interface IPipeMessageHandler<in TRequest, TResponse>
    where TRequest : notnull
    where TResponse : notnull
{
    static virtual UInt16 MessageType { get; }

    ValueTask<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken);
}
