using BitwardenApi.Models;

namespace BitwardenApi.Contracts;

public interface IVaultApiClient
{
    Task<DateTimeOffset> GetRevisionDateAsync(
        CancellationToken cancellationToken = default);

    Task GetSyncAsync(
        Func<Stream, Task> streamHandler,
        CancellationToken cancellationToken = default);

    Task GetCipherAsync(
        CipherId cipherId,
        Func<Stream, Task> streamHandler,
        CancellationToken cancellationToken = default);

    Task DeleteCipherAsync(
        CipherId cipherId,
        CancellationToken cancellationToken = default);
}
