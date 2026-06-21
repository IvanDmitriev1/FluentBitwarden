using FluentBitwarden.Contracts.Modules.Passkey;
using FluentBitwarden.Contracts.Modules.Passkey.Models;

namespace FluentBitwarden.AppHost.Modules.Passkey;

internal sealed class PasskeyAssertionService(
    IPasskeyCredentialSelectionClient passkeyCredentialSelectionClient)
{
    public async ValueTask<PasskeyAssertionResponse> SelectCredentialAsync(
        PasskeyGetAssertionRequest request,
        CancellationToken cancellationToken)
    {
        var credential = await passkeyCredentialSelectionClient.SelectPasskeyCredentialAsync(
            request,
            cancellationToken);

        var authenticatorData = WebAuthnAssertion.BuildAuthenticatorData(
            request.RpIdHash,
            credential.Counter,
            true,
            true);

        var signedPayload = WebAuthnAssertion.BuildSignedPayload(
            authenticatorData,
            request.ClientDataHash);

        var signature = WebAuthnAssertion.SignEs256(credential.KeyValue, signedPayload);

        return new PasskeyAssertionResponse
        {
            CredentialId = credential.CredentialId,
            UserId = credential.UserHandle,
            AuthenticatorData = authenticatorData,
            Signature = signature,
            UserName = credential.UserName,
            UserDisplayName = credential.UserDisplayName
        };
    }
}
