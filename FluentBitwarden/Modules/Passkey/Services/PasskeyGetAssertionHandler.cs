using System.Linq;
using BitwardenApi.Modules.Vault.Models;
using FluentBitwarden.Modules.Passkey.Internal;
using FluentBitwarden.Modules.Passkey.Models;
using FluentBitwarden.Modules.Vault.Abstractions;
using FluentBitwarden.Shared.Ipc.Abstractions;

namespace FluentBitwarden.Modules.Passkey.Services;

[Fody.ConfigureAwait(false)]
internal class PasskeyGetAssertionHandler(IVaultSyncService service) : IPipeMessageHandler<PasskeyGetAssertionRequest, PasskeyAssertionResponse>
{
    public ushort MessageType => 2;

    public ValueTask<PasskeyAssertionResponse> HandleAsync(PasskeyGetAssertionRequest request, CancellationToken cancellationToken)
    {
        var credential = service.Ciphers.OfType<LoginCipher>()
            .SelectMany(static l => l.Fido2Credentials)
            .FirstOrDefault(c => c.RpId == request.RpId);

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

        return ValueTask.FromResult(response);
    }
}
