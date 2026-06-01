using FluentBitwarden.Contracts.Modules.Passkey.Models;

namespace FluentBitwarden.Contracts.Modules.Passkey;

public interface IPasskeyClient
{
    ValueTask<PasskeyAssertionResponse> SelectCredentialAsync(
        PasskeyGetAssertionRequest request,
        CancellationToken cancellationToken);
}
