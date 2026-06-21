using FluentBitwarden.Contracts.Modules.Passkey.Models;

namespace FluentBitwarden.Contracts.Modules.Passkey;

public interface IPasskeyCredentialSelectionClient
{
    ValueTask<Fido2Credential> SelectPasskeyCredentialAsync(
        PasskeyGetAssertionRequest request,
        CancellationToken cancellationToken);
}
