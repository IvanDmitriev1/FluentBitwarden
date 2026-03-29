using BitwardenApi.Modules.Identity.Models;
using FluentBitwarden.Modules.Vault.Models;

namespace FluentBitwarden.Modules.Vault.Abstractions;

public interface IVaultUnlockService
{
    Task<UnlockCapabilities> GetCapabilitiesAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<UnlockResult> UnlockWithMasterPasswordAsync(
        UserId userId,
        ReadOnlyMemory<char> masterPassword,
        CancellationToken cancellationToken = default);

    Task<UnlockResult> UnlockWithPinAsync(
        UserId userId,
        ReadOnlyMemory<char> pin,
        CancellationToken cancellationToken = default);

    Task<UnlockResult> UnlockWithWindowsHelloAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

}