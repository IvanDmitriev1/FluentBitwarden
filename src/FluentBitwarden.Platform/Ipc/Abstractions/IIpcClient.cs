namespace FluentBitwarden.Platform.Ipc.Abstractions;

public interface IIpcClient
{
    Task<TResponse> SendAsync<TRequest,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : IIpcRequestMessage;
}
