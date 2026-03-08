using FluentBitwarden.Models;
using FluentBitwarden.Security;

namespace FluentBitwarden.Abstractions;

public interface ILocalUnlockService
{
    ValueTask<LocalUnlockStatus> GetStatusAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    ValueTask EnrollAsync(
        string accountId,
        byte[] userKey,
        UnlockEnrollmentSelection selection,
        CancellationToken cancellationToken = default);

    ValueTask<byte[]> UnlockWithWindowsHelloAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    ValueTask<byte[]> UnlockWithPinAsync(
        string accountId,
        string pin,
        CancellationToken cancellationToken = default);

    ValueTask ClearAsync(CancellationToken cancellationToken = default);
}
