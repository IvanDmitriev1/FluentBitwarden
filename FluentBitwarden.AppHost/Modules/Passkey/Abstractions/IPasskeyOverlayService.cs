using BitwardenApi.Models;
using FluentBitwarden.Modules.Passkey.Models;

namespace FluentBitwarden.Modules.Passkey.Abstractions;

public interface IPasskeyOverlayService
{
    Task<Fido2Credential> UnlockAndSelectAsync(
        PasskeyGetAssertionRequest request,
        CancellationToken cancellationToken);
}
