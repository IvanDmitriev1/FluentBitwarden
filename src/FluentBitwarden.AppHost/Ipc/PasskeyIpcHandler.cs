using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Infrastructure.WebAuthn;
using FluentBitwarden.Contracts.Infrastructure.UserDialog;
using FluentBitwarden.Contracts.Integrations.Passkey;
using FluentBitwarden.Contracts.Modules.Passkey.Models;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.Ipc;

internal sealed class PasskeyIpcHandler(
    IAppSessionAccessor appSessionAccessor,
    IAppSessionOperations appSessionOperations,
    IUserDialogService userDialogService) : IPasskeyClient, IIpcRequestsHandler
{
    public async Task<PasskeyAssertionResponse> SelectCredentialAsync(PasskeyGetAssertionRequest request, CancellationToken cancellationToken)
    {
        await appSessionOperations.WaitUntilUnlockedAsync(cancellationToken);
        _ = appSessionAccessor.RequireUnlockedState();

        var result =
            await userDialogService.ShowAsync<Fido2Credential, PasskeySelectCredentialRequest>(
                new PasskeySelectCredentialRequest(request.RpId),
                cancellationToken);

        var credential = result.GetResultOrThrow();

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

    public Task<PasskeyMakeCredentialResponse> MakeCredentialAsync(PasskeyMakeCredentialRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
