using FluentBitwarden.Contracts.Modules.Passkey.Models;

namespace FluentBitwarden.Contracts.Modules.Passkey;

public interface IPasskeyClient
{
    ValueTask<Fido2Credential> SelectCredentialAsync(
        PasskeyGetAssertionRequest request,
        CancellationToken cancellationToken);
}
