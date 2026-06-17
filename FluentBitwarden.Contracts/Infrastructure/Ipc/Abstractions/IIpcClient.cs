namespace FluentBitwarden.Contracts.Infrastructure.Ipc.Abstractions;

public interface IIpcClient
{
    ValueTask<TResponse> SendAsync<TRequest,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : IIpcRequestMessage;

    ValueTask<TResponse> SendAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    TResponse>(
        ushort messageType,
        CancellationToken cancellationToken = default);
}
