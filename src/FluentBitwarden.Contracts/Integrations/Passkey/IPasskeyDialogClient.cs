using FluentBitwarden.Contracts.Modules.Passkey.Models;

namespace FluentBitwarden.Contracts.Integrations.Passkey;

public interface IPasskeyDialogClient
{
    ValueTask<Fido2Credential> ShowPasskeySelectionDialogAsync(
        PasskeySelectCredentialRequest request,
        CancellationToken cancellationToken = default);
}
