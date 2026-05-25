using FluentBitwarden.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Modules.Passkey.Abstractions;
using FluentBitwarden.Modules.Passkey.Internal;
using FluentBitwarden.Modules.Passkey.Models;

namespace FluentBitwarden.Modules.Passkey.Services;

[Fody.ConfigureAwait(false)]
internal sealed class PasskeyAssertionHandler(IPasskeyOverlayService passkeyOverlayService) : IPipeRequestHandler<PasskeyGetAssertionRequest, PasskeyAssertionResponse>
{
    public async ValueTask<PasskeyAssertionResponse> HandleAsync(PasskeyGetAssertionRequest request, CancellationToken cancellationToken)
    {
        var credential = await passkeyOverlayService.UnlockAndSelectAsync(request, cancellationToken);
        if (credential is null)
        {
            throw new InvalidOperationException("Credential not found.");
        }

        var authenticatorData = WebAuthnAssertion.BuildAuthenticatorData(request.RpIdHash, credential.Counter, true, true);
        var signedPayload = WebAuthnAssertion.BuildSignedPayload(authenticatorData, request.ClientDataHash);

        var signature = WebAuthnAssertion.SignEs256(
            credential.KeyValue,
            signedPayload);

        var response = new PasskeyAssertionResponse
        {
            CredentialId = credential.CredentialId,
            UserId = credential.UserHandle,
            AuthenticatorData = authenticatorData,
            Signature = signature,
            UserName = credential.UserName,
            UserDisplayName = credential.UserDisplayName
        };

        return response;
    }
}
