using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.Contracts.Ipc.Abstractions;

public interface IIpcClient
{
    ValueTask<IpcResult<TResponse>> SendAsync<TRequest, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : IIpcRequestMessage
        where TResponse : notnull;

    ValueTask<IpcResult<TResponse>> SendAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResponse>(
        ushort messageType,
        CancellationToken cancellationToken = default)
        where TResponse : notnull;
}
