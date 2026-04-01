using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Security.Models.Unlock;

namespace FluentBitwarden.Modules.Security.Abstractions;

public readonly record struct AppPinUnlockRequest(string Pin) : IUnlockRequest;

internal interface IAppPinSecurityService : IUnlockStrategy<AppPinUnlockRequest>
{
    ValueTask SetUp(
        StoredAccount account,
        string pin,
        CancellationToken cancellationToken = default);

    ValueTask RemoveAsync(
        StoredAccount account,
        CancellationToken cancellationToken = default);

}