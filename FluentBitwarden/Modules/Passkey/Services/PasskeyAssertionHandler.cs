using FluentBitwarden.Modules.Passkey.Abstractions;
using FluentBitwarden.Modules.Passkey.Internal;
using FluentBitwarden.Modules.Passkey.Models;
using FluentBitwarden.Shared.Ipc.Abstractions;

namespace FluentBitwarden.Modules.Passkey.Services;

[Fody.ConfigureAwait(false)]
internal class PasskeyAssertionHandler(IPasskeyOverlayService passkeyOverlayService) : IPipeMessageHandler<PasskeyGetAssertionRequest, PasskeyAssertionResponse>
{
    public static ushort MessageType => 2;

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
