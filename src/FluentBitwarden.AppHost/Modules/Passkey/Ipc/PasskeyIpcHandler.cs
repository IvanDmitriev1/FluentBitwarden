using FluentBitwarden.AppHost.Modules.Passkey.Internal;
using FluentBitwarden.Contracts.Modules.Passkey;
using FluentBitwarden.Contracts.Modules.Passkey.Models;

namespace FluentBitwarden.AppHost.Modules.Passkey.Ipc;

internal sealed class PasskeyIpcHandler(IPasskeyDialogClient passkeyDialogClient)
    : IPasskeyClient, IIpcRequestsHandler
{
    public async ValueTask<PasskeyAssertionResponse> SelectCredentialAsync(PasskeyGetAssertionRequest request, CancellationToken cancellationToken)
    {
        var credential =
            await passkeyDialogClient.ShowPasskeySelectionDialogAsync(
                new PasskeySelectCredentialRequest(request.RpId),
                cancellationToken);

        var (authenticatorData, signature) =
            WebAuthnAssertion.Create(
                request.RpIdHash,
                request.ClientDataHash,
                credential.KeyValue,
                credential.Counter);

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

    public ValueTask<PasskeyMakeCredentialResponse> MakeCredentialAsync(PasskeyMakeCredentialRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
