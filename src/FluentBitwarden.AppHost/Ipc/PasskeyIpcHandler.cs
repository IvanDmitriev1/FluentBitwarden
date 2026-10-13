using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.AppHost.Infrastructure.Processes;
using FluentBitwarden.AppHost.Infrastructure.WebAuthn;
using FluentBitwarden.Contracts.Integrations.Passkey;
using FluentBitwarden.Contracts.Modules.Passkey.Models;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.Ipc;

internal sealed class PasskeyIpcHandler(IPasskeyDialogClient passkeyDialogClient) : IPasskeyClient, IIpcRequestsHandler
{
    public async Task<PasskeyAssertionResponse> SelectCredentialAsync(PasskeyGetAssertionRequest request, CancellationToken cancellationToken)
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

    public Task<PasskeyMakeCredentialResponse> MakeCredentialAsync(PasskeyMakeCredentialRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

//TODO remove after refactoring UI dialog system
internal sealed class PasskeyDialogClient(
    IIpcClient ipcClient,
    IUiProcessLauncher uiProcessLauncher) : IPasskeyDialogClient
{
    public Task<Fido2Credential> ShowPasskeySelectionDialogAsync(
        PasskeySelectCredentialRequest request,
        CancellationToken cancellationToken = default)
    {
        uiProcessLauncher.Activate();

        return ipcClient.SendAsync<PasskeySelectCredentialRequest, Fido2Credential>(
            request,
            cancellationToken);
    }
}
