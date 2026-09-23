using FluentBitwarden.Contracts.Modules.Passkey.Models;

namespace FluentBitwarden.Contracts.Integrations.Passkey;

public interface IPasskeyClient
{
    Task<PasskeyAssertionResponse> SelectCredentialAsync(
        PasskeyGetAssertionRequest request,
        CancellationToken cancellationToken);

    Task<PasskeyMakeCredentialResponse> MakeCredentialAsync(
        PasskeyMakeCredentialRequest request, CancellationToken cancellationToken);
}
