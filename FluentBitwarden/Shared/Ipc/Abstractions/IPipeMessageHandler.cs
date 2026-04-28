namespace FluentBitwarden.Shared.Ipc.Abstractions;

public interface IPipeMessageHandler<in TRequest, TResponse>
    where TRequest : notnull
    where TResponse : notnull
{
    UInt16 MessageType { get; }

    ValueTask<IpcResult<TResponse>> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken);
}
