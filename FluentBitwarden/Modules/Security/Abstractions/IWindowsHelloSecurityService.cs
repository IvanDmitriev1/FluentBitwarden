using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Security.Models;
using FluentBitwarden.Modules.Security.Models.Unlock;

namespace FluentBitwarden.Modules.Security.Abstractions;

public readonly record struct WindowsHelloUnlockRequest : IUnlockRequest;

internal interface IWindowsHelloSecurityService : IUnlockStrategy<WindowsHelloUnlockRequest>
{
    ValueTask<WindowsHelloAvailability> GetAvailabilityAsync(CancellationToken cancellationToken = default);

    ValueTask<bool> EnableAsync(
        StoredAccount account,
        CancellationToken cancellationToken = default);

    ValueTask DisableAsync(
        StoredAccount account,
        CancellationToken cancellationToken = default);

}