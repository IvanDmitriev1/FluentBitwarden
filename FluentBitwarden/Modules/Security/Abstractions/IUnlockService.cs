using BitwardenApi.Modules.Identity.Models;
using FluentBitwarden.Modules.Security.Models.Unlock;

namespace FluentBitwarden.Modules.Security.Abstractions;

public interface IUnlockService
{
    Task<UnlockCapabilities> GetCapabilitiesAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<UnlockResult> UnlockAsync<TRequest>(
        UserId userId,
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct, IUnlockRequest;
}